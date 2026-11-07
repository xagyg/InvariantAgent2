using System.Globalization;
using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Conscience;
using InvariantAgent.Core.Model.Conscience;
using InvariantAgent.Core.Model.Control;
using InvariantAgent.Core.Model.Stability;

namespace InvariantAgent.Conscience;

/// <summary>
/// Deterministic first version of functional conscience (IBAAM 6, Section 6.2): prospective tension calibrated from the
/// agent's own governance history.
/// </summary>
/// <remarks>
/// <para>Four components, each in 0..1:</para>
/// <list type="bullet">
/// <item><b>Violation risk</b>: similarity-weighted rate at which similar past plans were blocked by the controls, pulled
/// towards the deployment base rate by a pseudo-count. The only component that is a calibrated probability.</item>
/// <item><b>Forecast drift</b>: at the plan point, the drift similar past plans produced; at the answer point, the drift
/// score of the proposed state (a dry run of the commit). Expressed as (score / bound)^2.</item>
/// <item><b>Trend pressure</b>: the current stability region (halved when measurement confidence is low).</item>
/// <item><b>Commitment conflict</b>: closeness of the plan to live declined commitments (similarity rescaled from the match
/// threshold to 1), reviewer commitments counting in full and the agent's own at
/// <see cref="ConscienceOptions.AgentCommitmentStrength"/>.</item>
/// </list>
/// <para>
/// The plan-specific components combine as <c>p = 1 - product((1 - v_i)^w_i)</c>, so one strong component is enough to
/// raise tension while weak ones accumulate. Trend pressure then amplifies it: <c>tension = 1 - (1 - p)^(1 + w x trend)</c>.
/// Unstable history makes real concerns weigh more but never stops a plan on its own. Tension is a score for stance
/// selection, not a probability.
/// </para>
/// <para>
/// Same plan + same history gives the same appraisal; a different history can give a different one. That history
/// dependence, not non-determinism, is what separates this from an invariant.
/// </para>
/// </remarks>
public sealed class HistoryCalibratedConscience : IConscience
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    private readonly ConscienceOptions _options;
    private readonly IValueJudge? _valueJudge;

    /// <summary>Creates the conscience.</summary>
    /// <param name="options">Thresholds and weights.</param>
    /// <param name="valueJudge">Optional value judge consulted only when no close precedent exists.</param>
    public HistoryCalibratedConscience(ConscienceOptions options, IValueJudge? valueJudge = null)
    {
        _options = options;
        _valueJudge = valueJudge;
    }

    /// <inheritdoc />
    public string Name => "history-calibrated-v1";

    /// <inheritdoc />
    public ConscienceAppraisal Appraise(ConscienceInput input)
    {
        var violation = ViolationRisk(input);
        var drift = ForecastDrift(input);
        var trend = TrendPressure(input);
        var conflict = CommitmentConflict(input, out var conflictCited, out var clearedBy);

        var parts = new[] { violation, drift, trend, conflict };

        var tension = Combine(parts);
        var stance = SelectStance(tension);

        // Reviewer decisions take precedence over the agent's own judgement (IBAAM 6, Section 6.4.2). A reviewer who cleared
        // this conduct lets it proceed to the controls, which still decide what is permitted.
        if (clearedBy != null && stance != ConscienceStance.Proceed)
        {
            stance = ConscienceStance.Proceed;
        }

        var cited = input.Precedents
            .Where(p => p.Outcome == PrecedentOutcome.Blocked)
            .OrderByDescending(p => p.Similarity)
            .Take(_options.MaxCitations)
            .Select(p => p.TransitionId)
            .ToList();

        var citedCommitments = conflictCited.ToList();

        if (clearedBy != null)
        {
            citedCommitments.Insert(0, clearedBy.Id);
        }

        var values = ValuesAtStake(parts);

        return new ConscienceAppraisal(
            Round(tension),
            parts,
            stance,
            Justify(stance, tension, parts, clearedBy),
            cited)
        {
            CitedCommitments = citedCommitments.Distinct().ToList(),
            ValuesAtStake = values,
            Conscience = Name
        };
    }

    private TensionComponent ViolationRisk(ConscienceInput input)
    {
        if (input.Point == AppraisalPoint.Answer)
        {
            return new TensionComponent(
                TensionKind.ViolationRisk, 0, _options.ViolationRiskWeight,
                "not applicable at the answer point: every control has already passed")
            {
                Applicable = false
            };
        }

        var labelled = input.Precedents.Where(p => p.Outcome != PrecedentOutcome.Withheld).ToList();

        if (labelled.Count == 0 && _valueJudge != null)
        {
            var judged = _valueJudge.Judge(input);

            if (judged != null)
            {
                return judged with
                {
                    Kind = TensionKind.ViolationRisk,
                    Weight = _options.ViolationRiskWeight,
                    Value = Clamp(judged.Value),
                    Detail = "value judge (no close precedent): " + judged.Detail
                };
            }
        }

        var weight = labelled.Sum(p => p.Similarity);
        var blocked = labelled.Where(p => p.Outcome == PrecedentOutcome.Blocked).ToList();
        var blockedWeight = blocked.Sum(p => p.Similarity);
        var alpha = _options.PriorPseudoCount;

        var risk = (blockedWeight + alpha * input.BaseRate) / (weight + alpha);

        string detail;

        if (labelled.Count == 0)
        {
            detail = $"no close precedent; deployment base rate {F(input.BaseRate)} over {input.LabelledHistory} labelled transitions";
        }
        else
        {
            var cites = blocked
                .OrderByDescending(p => p.Similarity)
                .Take(_options.MaxCitations)
                .Select(p => $"#{Short(p.TransitionId)} blocked by {string.Join("+", p.BlockedBy)}" +
                             $"{(p.Values.Count > 0 ? $" [{string.Join(", ", p.Values)}]" : "")}, sim {F(p.Similarity)}");

            detail = $"{blocked.Count} of {labelled.Count} similar plans were blocked by the controls" +
                     (blocked.Count > 0 ? $" ({string.Join("; ", cites)})" : "") +
                     $"; base rate {F(input.BaseRate)}";
        }

        return new TensionComponent(TensionKind.ViolationRisk, Clamp(risk), _options.ViolationRiskWeight, detail)
        {
            Values = blocked.SelectMany(p => p.Values).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
        };
    }

    private TensionComponent ForecastDrift(ConscienceInput input)
    {
        var bound = Math.Max(1, input.DriftBound);

        if (input.Point == AppraisalPoint.Answer)
        {
            var score = input.ForecastDriftScore ?? 0;
            var value = DriftPressure(score, bound);

            return new TensionComponent(
                TensionKind.ForecastDrift, value, _options.ForecastDriftWeight,
                $"committing would produce drift score {score} against bound {bound}")
            {
                Values = value > 0 ? new[] { ProtectedValues.IdentityStability } : Array.Empty<string>()
            };
        }

        var withDrift = input.Precedents.Where(p => p.RealisedDrift.HasValue).ToList();

        if (withDrift.Count == 0)
        {
            return new TensionComponent(
                TensionKind.ForecastDrift, 0, _options.ForecastDriftWeight,
                "no similar plan has reached reduction, so no drift is forecast");
        }

        var weight = withDrift.Sum(p => p.Similarity);
        var mean = withDrift.Sum(p => p.Similarity * p.RealisedDrift!.Value) / weight;
        var forecast = DriftPressure(mean, bound);

        return new TensionComponent(
            TensionKind.ForecastDrift, forecast, _options.ForecastDriftWeight,
            $"similar plans produced drift {mean.ToString("0", Invariant)} on average ({withDrift.Count} precedents) against bound {bound}")
        {
            Values = forecast > 0 ? new[] { ProtectedValues.IdentityStability } : Array.Empty<string>()
        };
    }

    // Quadratic in the fraction of the bound used: ordinary adaptation (a goal change scores 40 of 70, giving 0.33) stays
    // mild, while drift close to the bound (60 of 70, giving 0.73) weighs heavily. Remaining tolerance shrinks fastest
    // near the bound.
    private static double DriftPressure(double score, int bound)
    {
        var fraction = Clamp(score / bound);

        return Clamp(fraction * fraction);
    }

    private TensionComponent TrendPressure(ConscienceInput input)
    {
        var trend = input.Trend ?? new StabilityAssessment();

        var value = trend.Region switch
        {
            StabilityRegion.Stable => 0.0,
            StabilityRegion.Watch => 0.34,
            StabilityRegion.Intervention => 0.67,
            StabilityRegion.Critical => 1.0,
            _ => 0.0
        };

        // Little history means little to go on: halve the pressure rather than act on a noisy region.
        if (trend.Confidence == MeasurementConfidence.Low)
        {
            value *= 0.5;
        }

        return new TensionComponent(
            TensionKind.TrendPressure, Clamp(value), _options.TrendPressureWeight,
            $"stability region {trend.Region} ({trend.Confidence} confidence, drift magnitude {trend.DriftMagnitude})")
        {
            Values = value > 0 ? new[] { ProtectedValues.IdentityStability } : Array.Empty<string>()
        };
    }

    private TensionComponent CommitmentConflict(
        ConscienceInput input,
        out IReadOnlyList<Guid> cited,
        out Commitment? clearedBy)
    {
        var capability = input.Proposed?.Capability ?? "";
        var planInput = input.Proposed?.Input ?? "";

        var matches = input.Commitments
            .Select(c => (Commitment: c, Similarity: CommitmentLedger.Match(c, capability, planInput)))
            .Where(m => m.Similarity >= _options.CommitmentMatchThreshold)
            .ToList();

        var conflicts = matches
            .Where(m => m.Commitment.Kind == CommitmentKind.Declined)
            .Select(m => (m.Commitment, m.Similarity,
                Score: Closeness(m.Similarity) *
                       (m.Commitment.Origin == CommitmentOrigin.Reviewer ? 1.0 : _options.AgentCommitmentStrength)))
            .OrderByDescending(m => m.Score)
            .ToList();

        var clearing = matches
            .Where(m => m.Commitment.Kind == CommitmentKind.Undertaken && m.Commitment.Origin == CommitmentOrigin.Reviewer)
            .OrderByDescending(m => m.Similarity)
            .FirstOrDefault();

        // A reviewer clearance applies unless a reviewer decline matches the plan at least as closely.
        var strongestReviewerDecline = conflicts
            .Where(c => c.Commitment.Origin == CommitmentOrigin.Reviewer)
            .Select(c => c.Similarity)
            .DefaultIfEmpty(0)
            .Max();

        clearedBy = clearing.Commitment != null && clearing.Similarity > strongestReviewerDecline
            ? clearing.Commitment
            : null;

        cited = conflicts.Take(_options.MaxCitations).Select(c => c.Commitment.Id).ToList();

        if (conflicts.Count == 0)
        {
            var detail = $"no live commitment conflicts with this plan ({input.Commitments.Count} live)";

            if (clearedBy != null)
            {
                detail += $"; cleared by reviewer commitment #{Short(clearedBy.Id)}";
            }

            return new TensionComponent(TensionKind.CommitmentConflict, 0, _options.CommitmentConflictWeight, detail);
        }

        var top = conflicts[0];

        return new TensionComponent(
            TensionKind.CommitmentConflict, Clamp(top.Score), _options.CommitmentConflictWeight,
            $"conflicts with {top.Commitment.Origin.ToString().ToLowerInvariant()} commitment #{Short(top.Commitment.Id)} " +
            $"(\"{top.Commitment.Statement}\", sim {F(top.Similarity)})" +
            (conflicts.Count > 1 ? $" and {conflicts.Count - 1} more" : ""))
        {
            Values = conflicts.Select(c => c.Commitment.Value).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
        };
    }

    // How close a match is, rescaled so a plan just at the match threshold contributes almost nothing and the identical
    // plan contributes in full. Without this, anything that merely cleared the threshold (same capability, same memory
    // key) scored about 0.5 straight away, so one commitment about "goal=learn a song" escalated "goal=learn the piano".
    private double Closeness(double similarity)
    {
        var threshold = Math.Min(0.99, _options.CommitmentMatchThreshold);

        return Clamp((similarity - threshold) / (1.0 - threshold));
    }

    // Plan-specific components (violation risk, forecast drift, commitment conflict) combine as
    // 1 - product((1 - v)^w). Trend pressure is remaining tolerance, not a concern about this plan, so it amplifies
    // that tension rather than adding its own: tension = 1 - (1 - planTension)^(1 + w x trend). A plan with no specific
    // concern stays at zero however unstable recent history is; how to respond to instability as such is the stability
    // evaluator's business (IBAAM 2), not the conscience's.
    private static double Combine(IEnumerable<TensionComponent> parts)
    {
        var remaining = 1.0;
        var amplification = 1.0;

        foreach (var part in parts.Where(p => p.Applicable && p.Weight > 0))
        {
            if (part.Kind == TensionKind.TrendPressure)
            {
                amplification += part.Weight * Clamp(part.Value);
                continue;
            }

            remaining *= Math.Pow(1.0 - Clamp(part.Value), part.Weight);
        }

        return Clamp(1.0 - Math.Pow(remaining, amplification));
    }

    private ConscienceStance SelectStance(double tension)
    {
        if (tension >= _options.VetoThreshold)
        {
            return ConscienceStance.Veto;
        }

        if (tension >= _options.EscalateThreshold)
        {
            return ConscienceStance.Escalate;
        }

        if (tension >= _options.ReviseThreshold)
        {
            return ConscienceStance.Revise;
        }

        return ConscienceStance.Proceed;
    }

    // Values ordered by how much their component contributed; falls back to the dominant component's tag.
    private static IReadOnlyList<string> ValuesAtStake(IReadOnlyList<TensionComponent> parts)
    {
        var ordered = parts
            .Where(p => p.Applicable)
            .OrderByDescending(p => p.Value * p.Weight)
            .ToList();

        var values = ordered
            .Where(p => p.Value > 0)
            .SelectMany(p => p.Values)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (values.Count > 0)
        {
            return values;
        }

        var dominant = ordered.FirstOrDefault();

        return dominant?.Kind switch
        {
            TensionKind.ForecastDrift or TensionKind.TrendPressure => new[] { ProtectedValues.IdentityStability },
            TensionKind.CommitmentConflict => new[] { ProtectedValues.Consistency },
            _ => new[] { ProtectedValues.ControlBoundary }
        };
    }

    // The justification is generated from the parts, so the stated reason always matches the computed score.
    private static string Justify(
        ConscienceStance stance,
        double tension,
        IReadOnlyList<TensionComponent> parts,
        Commitment? clearedBy)
    {
        var text = $"{stance} (tension {F(tension)}): " + string.Join("; ", parts.Select(p =>
            $"{Label(p.Kind)} {(p.Applicable ? F(p.Value) : "n/a")} - {p.Detail}"));

        if (clearedBy != null)
        {
            text += $". A reviewer cleared this conduct (commitment #{Short(clearedBy.Id)}: \"{clearedBy.Statement}\"), " +
                    "so it proceeds to the controls";
        }

        return text + ".";
    }

    private static string Label(TensionKind kind) => kind switch
    {
        TensionKind.ViolationRisk => "violation risk",
        TensionKind.ForecastDrift => "forecast drift",
        TensionKind.TrendPressure => "trend pressure",
        TensionKind.CommitmentConflict => "commitment conflict",
        _ => kind.ToString()
    };

    private static double Clamp(double value) => double.IsNaN(value) ? 0 : Math.Max(0, Math.Min(1, value));

    private static double Round(double value) => Math.Round(value, 4);

    private static string F(double value) => value.ToString("0.00", Invariant);

    private static string Short(Guid id) => id.ToString("N")[..8];
}

using InvariantAgent.Core.Conscience;
using InvariantAgent.Core.Drift;
using InvariantAgent.Core.Model.Agent;
using InvariantAgent.Core.Model.Conscience;
using InvariantAgent.Core.Model.Control;
using InvariantAgent.Core.Model.Transition;

namespace InvariantAgent.Conscience;

/// <summary>
/// Retrieves similar past transitions and labels them by how the controls treated them (IBAAM 6, Sections 5 and 6.4.1).
/// </summary>
/// <remarks>
/// Every transition the controls blocked is a boundary label; every completed one is a negative label. Transitions the
/// conscience itself stopped carry no boundary label (the controls never judged the whole of them), so they are marked
/// <see cref="PrecedentOutcome.Withheld"/> and excluded from violation risk. This keeps the conscience from learning its
/// own vetoes as if the controls had made them (the feedback loop in IBAAM 6, Section 9).
/// <para>
/// Transitions blocked only by plan-scope invariants are not evidence either. The conscience appraises a plan only after
/// it has passed the plan invariants, so another plan's plan-scope rejection (an unknown capability, a forbidden delete)
/// says nothing about whether this one will be blocked. Counting them let a mistyped command (<c>set-memory goal=x</c>,
/// rejected as an unknown capability) raise violation risk for the correct one (<c>memory-set goal=x</c>).
/// </para>
/// </remarks>
public sealed class PrecedentIndex
{
    private readonly IReadOnlyDictionary<string, string> _valueByInvariant;
    private readonly IReadOnlyDictionary<string, InvariantScope> _scopeByInvariant;
    private readonly BehaviouralDriftDetector _detector;
    private readonly ConscienceOptions _options;

    /// <summary>Creates the index.</summary>
    /// <param name="valueByInvariant">Protected-value tag of each registered invariant, by invariant name.</param>
    /// <param name="detector">Detector used to score the drift past transitions produced.</param>
    /// <param name="options">Similarity threshold and precedent count.</param>
    /// <param name="scopeByInvariant">
    /// Lifecycle scope of each registered invariant, by name. Used to ignore plan-scope rejections; when omitted, every
    /// rejection counts.
    /// </param>
    public PrecedentIndex(
        IReadOnlyDictionary<string, string> valueByInvariant,
        BehaviouralDriftDetector detector,
        ConscienceOptions options,
        IReadOnlyDictionary<string, InvariantScope>? scopeByInvariant = null)
    {
        _valueByInvariant = valueByInvariant;
        _scopeByInvariant = scopeByInvariant ?? new Dictionary<string, InvariantScope>();
        _detector = detector;
        _options = options;
    }

    /// <summary>Most similar precedents for <paramref name="action"/>, most similar (then most recent) first.</summary>
    /// <param name="history">Transition history, oldest first.</param>
    /// <param name="action">The plan being appraised.</param>
    public IReadOnlyList<Precedent> Find(IReadOnlyList<Transition> history, AgentAction action)
    {
        var candidates = new List<(Transition Transition, double Similarity, int Index)>();

        for (var i = 0; i < history.Count; i++)
        {
            var transition = history[i];

            if (!IsCandidate(transition))
            {
                continue;
            }

            var similarity = PlanSimilarity.Score(
                action.Capability, action.Input,
                transition.ProposedAction.Capability, transition.ProposedAction.Input);

            if (similarity >= _options.MinPrecedentSimilarity)
            {
                candidates.Add((transition, similarity, i));
            }
        }

        return candidates
            .OrderByDescending(c => c.Similarity)
            .ThenByDescending(c => c.Index)
            .Take(_options.MaxPrecedents)
            .Select(c => ToPrecedent(c.Transition, c.Similarity))
            .ToList();
    }

    /// <summary>
    /// Smoothed rate at which the controls blocked plans across the whole history (the cold-start prior), and the
    /// number of labelled transitions it is based on.
    /// </summary>
    public (double Rate, int Labelled) BaseRate(IReadOnlyList<Transition> history)
    {
        var labelled = 0;
        var blocked = 0;

        foreach (var transition in history.Where(IsCandidate))
        {
            var outcome = Classify(transition);

            if (outcome == PrecedentOutcome.Withheld)
            {
                continue;
            }

            labelled++;

            if (outcome == PrecedentOutcome.Blocked)
            {
                blocked++;
            }
        }

        var alpha = _options.BaseRatePriorAlpha;
        var beta = _options.BaseRatePriorBeta;

        return ((blocked + alpha) / (labelled + alpha + beta), labelled);
    }

    /// <summary>How the controls treated a past transition.</summary>
    public static PrecedentOutcome Classify(Transition transition)
    {
        var stop = transition.Appraisals?.LastOrDefault(a => a.Stopped);

        if (stop != null && (transition.BlockingInvariants == null || transition.BlockingInvariants.Count == 0))
        {
            // Stopped at the answer point: every control had already passed, so the boundary label is "passed".
            // Stopped at the plan point: the controls never saw the rest of it, so there is no label.
            return stop.Point == AppraisalPoint.Answer ? PrecedentOutcome.Passed : PrecedentOutcome.Withheld;
        }

        return transition.Status == TransitionStatus.Rejected ? PrecedentOutcome.Blocked : PrecedentOutcome.Passed;
    }

    /// <summary>Invariants that blocked a transition (falls back to parsing the reason for older history).</summary>
    public IReadOnlyList<string> BlockedBy(Transition transition)
    {
        if (transition.BlockingInvariants != null && transition.BlockingInvariants.Count > 0)
        {
            return transition.BlockingInvariants;
        }

        // History recorded before BlockingInvariants existed: the reason is "Invariant: reason" per line.
        return (transition.Reason ?? "")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line => line.Split(':', 2)[0].Trim())
            .Where(name => _valueByInvariant.ContainsKey(name))
            .Distinct()
            .ToList();
    }

    /// <summary>
    /// <c>true</c> if the transition was rejected only by plan-scope invariants. A plan under appraisal has already passed
    /// those, so such a rejection is not evidence about it.
    /// </summary>
    public bool BlockedOnlyAtPlanScope(Transition transition)
    {
        if (transition.Status != TransitionStatus.Rejected || Classify(transition) != PrecedentOutcome.Blocked)
        {
            return false;
        }

        var blockers = BlockedBy(transition);

        return blockers.Count > 0 &&
               blockers.All(name => _scopeByInvariant.TryGetValue(name, out var scope) && scope == InvariantScope.Plan);
    }

    private bool IsCandidate(Transition transition)
    {
        return transition.ProposedAction != null &&
               !BlockedOnlyAtPlanScope(transition) &&
               !transition.ProposedAction.HasError &&
               !string.IsNullOrWhiteSpace(transition.ProposedAction.Capability) &&
               !_options.ExemptCapabilities.Contains(transition.ProposedAction.Capability) &&
               (transition.Status == TransitionStatus.Completed || transition.Status == TransitionStatus.Rejected);
    }

    private Precedent ToPrecedent(Transition transition, double similarity)
    {
        var outcome = Classify(transition);
        var blockedBy = outcome == PrecedentOutcome.Blocked ? BlockedBy(transition) : Array.Empty<string>();

        return new Precedent(
            transition.Id,
            Math.Round(similarity, 4),
            outcome,
            transition.ProposedAction.Capability,
            transition.ProposedAction.Input ?? "")
        {
            BlockedBy = blockedBy,
            Values = blockedBy
                .Select(name => _valueByInvariant.TryGetValue(name, out var value) ? value : null)
                .Where(value => value != null)
                .Select(value => value!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            RealisedDrift = transition.After != null ? _detector.ScoreAsRecorded(transition).Score : null
        };
    }
}

using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Conscience;
using InvariantAgent.Core.Drift;
using InvariantAgent.Core.Model.Agent;
using InvariantAgent.Core.Model.Conscience;
using InvariantAgent.Core.Model.Control;
using InvariantAgent.Core.Model.Planning;
using InvariantAgent.Core.Model.Transition;

namespace InvariantAgent.Conscience;

/// <summary>
/// Connects an <see cref="IConscience"/> to the governed runtime: assembles its input from governance history, records
/// each appraisal on the transition, and creates commitments (IBAAM 6, Sections 5 and 6).
/// </summary>
/// <remarks>
/// The runtime decides what each stance does to the pipeline and calls this class only after the relevant invariants
/// have passed, so the conscience can never authorise anything the controls block.
/// </remarks>
public sealed class ConscienceCoordinator
{
    private readonly IConscience _conscience;
    private readonly ITransitionStore _transitions;
    private readonly ICommitmentStore _commitments;
    private readonly IStabilityEvaluator _stability;
    private readonly BehaviouralDriftDetector _detector;
    private readonly PrecedentIndex _precedents;

    /// <summary>Creates the coordinator.</summary>
    /// <param name="conscience">The conscience implementation.</param>
    /// <param name="transitions">Governance history (the source of precedents and calibration).</param>
    /// <param name="commitments">Commitment store.</param>
    /// <param name="stability">Stability evaluator for trend pressure.</param>
    /// <param name="detector">Drift detector for forecast drift.</param>
    /// <param name="invariants">The registered invariants, for their protected-value tags.</param>
    /// <param name="options">Conscience options.</param>
    /// <param name="session">Current session (for conversation-scoped commitments); a new one if omitted.</param>
    public ConscienceCoordinator(
        IConscience conscience,
        ITransitionStore transitions,
        ICommitmentStore commitments,
        IStabilityEvaluator stability,
        BehaviouralDriftDetector detector,
        IEnumerable<IInvariant> invariants,
        ConscienceOptions options,
        ConscienceSession? session = null)
    {
        _conscience = conscience;
        _transitions = transitions;
        _commitments = commitments;
        _stability = stability;
        _detector = detector;
        Options = options;
        ConversationId = (session ?? new ConscienceSession()).ConversationId;

        var valueByInvariant = new Dictionary<string, string>(StringComparer.Ordinal);
        var scopeByInvariant = new Dictionary<string, InvariantScope>(StringComparer.Ordinal);

        foreach (var invariant in invariants)
        {
            valueByInvariant[invariant.Name] = invariant.ProtectedValue;
            scopeByInvariant[invariant.Name] = invariant.Scope;
        }

        _precedents = new PrecedentIndex(valueByInvariant, detector, options, scopeByInvariant);
    }

    /// <summary>Conscience options in force.</summary>
    public ConscienceOptions Options { get; }

    /// <summary>
    /// Identifies this runtime session for conversation-scoped commitments. A restart starts a new conversation, while
    /// user- and deployment-scoped commitments carry over.
    /// </summary>
    public string ConversationId { get; }

    /// <summary><c>true</c> if <paramref name="action"/> should be appraised (not exempt and not a planner error).</summary>
    public bool ShouldAppraise(AgentAction? action)
    {
        return action != null &&
               !action.HasError &&
               !string.IsNullOrWhiteSpace(action.Capability) &&
               !Options.ExemptCapabilities.Contains(action.Capability);
    }

    /// <summary>The commitments live for this session right now.</summary>
    public IReadOnlyList<Commitment> LiveCommitments()
    {
        return CommitmentLedger.Live(
            _commitments.GetAll(), DateTimeOffset.UtcNow, ConversationId, Options.UserId, Options.CommitmentMatchThreshold);
    }

    /// <summary>Assembles the conscience input for the transition's current action.</summary>
    /// <param name="context">The transition being governed.</param>
    /// <param name="point">Plan or answer appraisal.</param>
    /// <param name="attempt">Revision attempt.</param>
    public ConscienceInput BuildInput(TransitionContext context, AppraisalPoint point, int attempt)
    {
        var transition = context.Transition;
        var history = _transitions.GetAll();
        var action = transition.ProposedAction;

        var precedents = _precedents.Find(history, action);
        var (baseRate, labelled) = _precedents.BaseRate(history);

        int? forecastDrift = null;

        // Answer point: the reducer has proposed the next state, so drift is a dry run of the commit, scored against the
        // baseline recorded on the transition (the same one the drift-bound invariant used).
        if (point == AppraisalPoint.Answer && transition.After != null)
        {
            forecastDrift = _detector.ScoreAsRecorded(transition).Score;
        }

        // Trend is measured without the conscience's own plan-point interventions: counting them as instability would let
        // each stop raise the pressure for the next (the self-reinforcing loop of IBAAM 6, Section 9).
        var trendHistory = history
            .Where(t => PrecedentIndex.Classify(t) != PrecedentOutcome.Withheld)
            .ToList();

        return new ConscienceInput(
            point,
            action,
            transition.Before,
            _stability.Evaluate(trendHistory),
            precedents,
            LiveCommitments())
        {
            BaseRate = baseRate,
            LabelledHistory = labelled,
            ForecastDriftScore = forecastDrift,
            DriftBound = BehaviouralDriftDetector.DefaultBoundThreshold,
            Outcome = point == AppraisalPoint.Answer ? transition.Outcome : null,
            Attempt = attempt
        };
    }

    /// <summary>Appraises the transition's current action.</summary>
    /// <param name="context">The transition being governed.</param>
    /// <param name="point">Plan or answer appraisal.</param>
    /// <param name="attempt">Revision attempt (0 for the first proposal).</param>
    public ConscienceAppraisal Appraise(TransitionContext context, AppraisalPoint point, int attempt)
    {
        var appraisal = _conscience.Appraise(BuildInput(context, point, attempt));

        return string.IsNullOrEmpty(appraisal.Conscience)
            ? appraisal with { Conscience = _conscience.Name }
            : appraisal;
    }

    /// <summary>
    /// Records an appraisal on the transition (for replay) with a Conscience event, and creates a commitment when the
    /// effect was Revised, Escalated or Vetoed.
    /// </summary>
    /// <param name="context">The transition being governed.</param>
    /// <param name="point">Plan or answer appraisal.</param>
    /// <param name="attempt">Revision attempt.</param>
    /// <param name="appraised">The action that was appraised.</param>
    /// <param name="appraisal">The appraisal.</param>
    /// <param name="effect">What the runtime did with it.</param>
    /// <param name="note">Why the effect differs from the stance, if it does.</param>
    public ConscienceAppraisalRecord Record(
        TransitionContext context,
        AppraisalPoint point,
        int attempt,
        AgentAction appraised,
        ConscienceAppraisal appraisal,
        ConscienceEffect effect,
        string note = "")
    {
        var transition = context.Transition;

        var commitment = CreateCommitment(transition, appraised, appraisal, effect);

        if (commitment != null)
        {
            _commitments.Append(commitment);
        }

        var record = new ConscienceAppraisalRecord
        {
            Point = point,
            Attempt = attempt,
            Capability = appraised?.Capability ?? "",
            Input = appraised?.Input ?? "",
            Appraisal = appraisal,
            Effect = effect,
            Note = note ?? "",
            CommitmentId = commitment?.Id
        };

        transition.Appraisals.Add(record);

        var message = $"{point} appraisal: {appraisal.Stance} (tension {appraisal.Tension:0.00}) -> {effect}" +
                      (string.IsNullOrWhiteSpace(note) ? "" : $" [{note}]");

        transition.AddEvent(TransitionEventStage.Conscience, message,
            new Dictionary<string, object>
            {
                ["Point"] = point.ToString(),
                ["Attempt"] = attempt,
                ["Capability"] = record.Capability,
                ["Stance"] = appraisal.Stance.ToString(),
                ["Effect"] = effect.ToString(),
                ["Tension"] = appraisal.Tension,
                ["ViolationRisk"] = PartValue(appraisal, TensionKind.ViolationRisk),
                ["ForecastDrift"] = PartValue(appraisal, TensionKind.ForecastDrift),
                ["TrendPressure"] = PartValue(appraisal, TensionKind.TrendPressure),
                ["CommitmentConflict"] = PartValue(appraisal, TensionKind.CommitmentConflict),
                ["ValuesAtStake"] = string.Join(",", appraisal.ValuesAtStake),
                ["Justification"] = appraisal.Justification,
                ["Commitment"] = commitment?.Id.ToString() ?? ""
            });

        return record;
    }

    /// <summary>Conscience feedback for the planner when a revised plan is requested.</summary>
    public static PlannerRevision ToRevision(int attempt, AgentAction previous, ConscienceAppraisal appraisal)
    {
        return new PlannerRevision
        {
            Attempt = attempt,
            Previous = previous,
            Tension = appraisal.Tension,
            Justification = appraisal.Justification,
            ValuesAtStake = appraisal.ValuesAtStake
        };
    }

    /// <summary>True if two actions are materially the same plan.</summary>
    public static bool SamePlan(AgentAction? a, AgentAction? b)
    {
        return a != null && b != null &&
               string.Equals(PlanSimilarity.Normalise(a.Capability), PlanSimilarity.Normalise(b.Capability), StringComparison.Ordinal) &&
               string.Equals(PlanSimilarity.Normalise(a.Input), PlanSimilarity.Normalise(b.Input), StringComparison.Ordinal);
    }

    private Commitment? CreateCommitment(
        Transition transition,
        AgentAction? appraised,
        ConscienceAppraisal appraisal,
        ConscienceEffect effect)
    {
        if (appraised == null ||
            (effect != ConscienceEffect.Revised && effect != ConscienceEffect.Escalated && effect != ConscienceEffect.Vetoed))
        {
            return null;
        }

        var scope = effect == ConscienceEffect.Revised ? Options.ReviseCommitmentScope : Options.StopCommitmentScope;
        var now = DateTimeOffset.UtcNow;
        var value = appraisal.ValuesAtStake.FirstOrDefault() ?? ProtectedValues.ControlBoundary;

        var verb = effect switch
        {
            ConscienceEffect.Revised => "Asked to revise",
            ConscienceEffect.Escalated => "Held for human review",
            _ => "Declined"
        };

        return new Commitment(
            Guid.NewGuid(),
            transition.Id,
            value,
            scope,
            $"{verb}: {appraised.Capability} {appraised.Input}".Trim(),
            CommitmentOrigin.Agent,
            now,
            Options.AgentCommitmentLifetime.HasValue ? now + Options.AgentCommitmentLifetime.Value : null)
        {
            Kind = CommitmentKind.Declined,
            Capability = appraised.Capability ?? "",
            Subject = PlanSimilarity.Normalise(appraised.Input),
            ConversationId = ConversationId,
            UserId = Options.UserId
        };
    }

    private static double PartValue(ConscienceAppraisal appraisal, TensionKind kind)
    {
        return appraisal.Parts.FirstOrDefault(p => p.Kind == kind)?.Value ?? 0;
    }
}

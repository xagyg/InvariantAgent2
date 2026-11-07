using InvariantAgent.Conscience;
using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Conscience;
using InvariantAgent.Core.Model.Capability;
using InvariantAgent.Core.Model.Conscience;
using InvariantAgent.Core.Model.Control;
using InvariantAgent.Core.Model.Data;

namespace InvariantAgent.Capabilities.Tools.Internal;

/// <summary>
/// <c>conscience-review &lt;transition-id-prefix&gt; uphold|overturn [note]</c>: a human reviewer resolves a conscience
/// escalation (or appeals a veto).
/// </summary>
/// <remarks>
/// <para>
/// The decision is recorded as a reviewer commitment, which takes precedence over the agent's own commitments
/// (IBAAM 6, Section 6.4.2) and is the value-learning signal the controls cannot provide (Section 5.2):
/// </para>
/// <list type="bullet">
/// <item><b>uphold</b>: the conduct is declined. Similar plans now meet a reviewer-strength commitment conflict.</item>
/// <item><b>overturn</b>: the conduct is cleared. Earlier declined commitments on the same subject are superseded and
/// similar plans proceed to the controls, which still decide what is permitted.</item>
/// </list>
/// <para>Nothing is re-run: the user retries the request if they still want it.</para>
/// </remarks>
public sealed class ConscienceReviewTool : ICapability
{
    private readonly ITransitionStore _transitions;
    private readonly ICommitmentStore _commitments;
    private readonly ConscienceOptions _options;

    /// <summary>Creates the tool.</summary>
    /// <param name="transitions">Transition history.</param>
    /// <param name="commitments">Commitment store the decision is written to.</param>
    /// <param name="options">Conscience options (reviewer commitment scope, user id).</param>
    public ConscienceReviewTool(ITransitionStore transitions, ICommitmentStore commitments, ConscienceOptions options)
    {
        _transitions = transitions;
        _commitments = commitments;
        _options = options;
    }

    /// <inheritdoc />
    public string Name => "conscience-review";

    /// <inheritdoc />
    /// <remarks>A reviewer decision is the value-learning signal the controls cannot provide, so it must come from a human,
    /// never from a planner reviewing its own escalation.</remarks>
    public bool OperatorOnly => true;

    /// <inheritdoc />
    public CapabilityResult Execute(CapabilityRequest request)
    {
        var parts = (request.Input ?? "").Split(' ', 3, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length < 2)
        {
            return CapabilityResult.Fail(Name, "Expected: conscience-review <transition-id-prefix> uphold|overturn [note]");
        }

        var prefix = parts[0].Replace("-", "");
        var decision = parts[1].ToLowerInvariant();
        var note = parts.Length > 2 ? parts[2] : "";

        if (decision != "uphold" && decision != "overturn")
        {
            return CapabilityResult.Fail(Name, "Decision must be 'uphold' or 'overturn'.");
        }

        var transition = _transitions.GetAll()
            .LastOrDefault(t => t.Id.ToString("N").StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

        var stop = transition?.Appraisals?.LastOrDefault(a => a.Stopped);

        if (transition == null || stop == null)
        {
            return CapabilityResult.Fail(Name, $"No escalated or vetoed transition matches '{parts[0]}'.");
        }

        var all = _commitments.GetAll();

        if (CommitmentLedger.IsResolved(all, transition.Id))
        {
            return CapabilityResult.Fail(Name, $"Transition {Short(transition.Id)} has already been reviewed.");
        }

        var upheld = decision == "uphold";
        var value = stop.Appraisal?.ValuesAtStake.FirstOrDefault() ?? ProtectedValues.ControlBoundary;
        var conduct = $"{stop.Capability} {stop.Input}".Trim();

        var commitment = new Commitment(
            Guid.NewGuid(),
            transition.Id,
            value,
            _options.ReviewerCommitmentScope,
            (upheld ? $"Reviewer declined: {conduct}" : $"Reviewer cleared: {conduct}") +
                (string.IsNullOrWhiteSpace(note) ? "" : $" ({note})"),
            CommitmentOrigin.Reviewer,
            DateTimeOffset.UtcNow,
            null)
        {
            Kind = upheld ? CommitmentKind.Declined : CommitmentKind.Undertaken,
            Capability = stop.Capability,
            Subject = PlanSimilarity.Normalise(stop.Input),
            UserId = _options.UserId,
            Resolves = transition.Id
        };

        _commitments.Append(commitment);

        return CapabilityResult.Ok(Name, new TextData
        {
            Value = upheld
                ? $"Upheld {stop.Effect.ToString().ToLowerInvariant()} of {Short(transition.Id)} ('{conduct}'). Reviewer commitment #{Short(commitment.Id)} [{value}] declines similar plans."
                : $"Overturned {stop.Effect.ToString().ToLowerInvariant()} of {Short(transition.Id)} ('{conduct}'). Reviewer commitment #{Short(commitment.Id)} [{value}] clears similar plans; retry the request to run it."
        });
    }

    private static string Short(Guid id) => id.ToString("N")[..8];
}

using System.Globalization;
using System.Text;
using InvariantAgent.Conscience;
using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Conscience;
using InvariantAgent.Core.Model.Capability;
using InvariantAgent.Core.Model.Conscience;
using InvariantAgent.Core.Model.Data;
using InvariantAgent.Core.Model.Transition;

namespace InvariantAgent.Capabilities.Tools.Internal;

/// <summary>
/// <c>conscience [commitments | pending | calibration | last | id-prefix]</c>: inspects the functional conscience
/// (IBAAM 6): live commitments, escalations awaiting review, violation-risk calibration and recorded appraisals.
/// </summary>
/// <remarks>Read-only and exempt from appraisal, so the conscience can always be inspected.</remarks>
public sealed class ConscienceShowTool : ICapability
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    private readonly ITransitionStore _transitions;
    private readonly ICommitmentStore _commitments;
    private readonly ConscienceOptions _options;
    private readonly ConscienceSession _session;

    /// <summary>Creates the tool.</summary>
    /// <param name="transitions">Transition history.</param>
    /// <param name="commitments">Commitment store.</param>
    /// <param name="options">Conscience options.</param>
    /// <param name="session">Current session, for conversation-scoped commitments.</param>
    public ConscienceShowTool(
        ITransitionStore transitions,
        ICommitmentStore commitments,
        ConscienceOptions options,
        ConscienceSession session)
    {
        _transitions = transitions;
        _commitments = commitments;
        _options = options;
        _session = session;
    }

    /// <inheritdoc />
    public string Name => "conscience";

    /// <inheritdoc />
    public IReadOnlyCollection<string> Aliases => new[] { "conscience-show" };

    /// <inheritdoc />
    public CapabilityResult Execute(CapabilityRequest request)
    {
        var arg = (request.Input ?? "").Trim();
        var history = _transitions.GetAll();

        string text = arg.ToLowerInvariant() switch
        {
            "" => Summary(history),
            "commitments" => Commitments(),
            "pending" => Pending(history),
            "calibration" => CalibrationReport.Compute(history).Format(),
            _ => Appraisals(history, arg)
        };

        return CapabilityResult.Ok(Name, new TextData { Value = text });
    }

    private string Summary(IReadOnlyList<Transition> history)
    {
        var sb = new StringBuilder();
        var all = _commitments.GetAll();
        var live = Live(all);
        var appraised = history.Where(t => t.Appraisals?.Count > 0).ToList();

        sb.AppendLine($"Conscience: {(_options.Enabled ? "enabled" : "disabled")}");
        sb.AppendLine($"Appraised transitions: {appraised.Count}");

        foreach (var group in appraised
                     .SelectMany(t => t.Appraisals)
                     .GroupBy(a => a.Effect)
                     .OrderBy(g => g.Key))
        {
            sb.AppendLine($"  {group.Key}: {group.Count()}");
        }

        sb.AppendLine($"Commitments: {live.Count} live of {all.Count} recorded");
        sb.AppendLine($"Escalations awaiting review: {PendingEscalations(history, all).Count}");
        sb.AppendLine();
        sb.AppendLine("Usage: conscience commitments | pending | calibration | last | <transition-id-prefix>");
        sb.AppendLine("       conscience-review <transition-id-prefix> uphold|overturn [note]");

        return sb.ToString().TrimEnd();
    }

    private string Commitments()
    {
        var all = _commitments.GetAll();
        var live = Live(all);

        if (all.Count == 0)
        {
            return "No commitments recorded.";
        }

        var sb = new StringBuilder();
        sb.AppendLine($"Live commitments ({live.Count} of {all.Count}):");

        foreach (var c in live)
        {
            sb.AppendLine(
                $"  #{Short(c.Id)} {c.Origin} {c.Kind} [{c.Value}] {c.Scope}: {c.Statement}" +
                $" (from #{Short(c.SourceTransition)}{(c.Expires.HasValue ? $", expires {c.Expires.Value:yyyy-MM-dd}" : "")})");
        }

        return sb.ToString().TrimEnd();
    }

    private string Pending(IReadOnlyList<Transition> history)
    {
        var pending = PendingEscalations(history, _commitments.GetAll());

        if (pending.Count == 0)
        {
            return "No escalations awaiting review.";
        }

        var sb = new StringBuilder();
        sb.AppendLine("Escalations awaiting review:");

        foreach (var (transition, record) in pending)
        {
            sb.AppendLine(
                $"  {Short(transition.Id)} {record.Point}: {record.Capability} {record.Input}".TrimEnd());
            sb.AppendLine($"    {record.Appraisal.Justification}");
        }

        sb.AppendLine();
        sb.AppendLine("Resolve with: conscience-review <id> uphold|overturn [note]");

        return sb.ToString().TrimEnd();
    }

    private static string Appraisals(IReadOnlyList<Transition> history, string arg)
    {
        var transition = arg.Equals("last", StringComparison.OrdinalIgnoreCase)
            ? history.LastOrDefault(t => t.Appraisals?.Count > 0)
            : history.LastOrDefault(t => t.Id.ToString("N").StartsWith(arg.Replace("-", ""), StringComparison.OrdinalIgnoreCase));

        if (transition == null)
        {
            return "Transition not found (or never appraised).";
        }

        if (transition.Appraisals == null || transition.Appraisals.Count == 0)
        {
            return $"Transition {Short(transition.Id)} was not appraised (exempt capability or conscience disabled).";
        }

        var sb = new StringBuilder();
        sb.AppendLine($"Transition {Short(transition.Id)}: {transition.Input} -> {transition.Status}");

        foreach (var record in transition.Appraisals)
        {
            var a = record.Appraisal;

            sb.AppendLine();
            sb.AppendLine($"{record.Point} appraisal, attempt {record.Attempt}: {record.Capability} {record.Input}".TrimEnd());
            sb.AppendLine($"  Stance: {a.Stance} -> {record.Effect}{(string.IsNullOrWhiteSpace(record.Note) ? "" : $" ({record.Note})")}");
            sb.AppendLine($"  Tension: {a.Tension.ToString("0.00", Invariant)}  Values at stake: {string.Join(", ", a.ValuesAtStake)}");

            foreach (var part in a.Parts)
            {
                sb.AppendLine(
                    $"  - {part.Kind}: {(part.Applicable ? part.Value.ToString("0.00", Invariant) : "n/a")} " +
                    $"(w {part.Weight.ToString("0.0", Invariant)}) {part.Detail}");
            }

            if (record.CommitmentId.HasValue)
            {
                sb.AppendLine($"  Commitment created: #{Short(record.CommitmentId.Value)}");
            }
        }

        return sb.ToString().TrimEnd();
    }

    private IReadOnlyList<Commitment> Live(IReadOnlyList<Commitment> all)
    {
        return CommitmentLedger.Live(
            all, DateTimeOffset.UtcNow, _session.ConversationId, _options.UserId, _options.CommitmentMatchThreshold);
    }

    /// <summary>Escalated transitions that no reviewer has resolved yet.</summary>
    internal static IReadOnlyList<(Transition Transition, ConscienceAppraisalRecord Record)> PendingEscalations(
        IReadOnlyList<Transition> history,
        IReadOnlyList<Commitment> commitments)
    {
        return history
            .Select(t => (Transition: t, Record: t.Appraisals?.LastOrDefault(a => a.Effect == ConscienceEffect.Escalated)))
            .Where(x => x.Record != null && !CommitmentLedger.IsResolved(commitments, x.Transition.Id))
            .Select(x => (x.Transition, x.Record!))
            .ToList();
    }

    private static string Short(Guid id) => id.ToString("N")[..8];
}

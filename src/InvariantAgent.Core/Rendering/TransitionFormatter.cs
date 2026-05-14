namespace InvariantAgent.Core.Rendering;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using InvariantAgent.Core.Model.Transition;

public static class TransitionFormatter
{
    public static string FormatReplay(IEnumerable<Transition> transitions)
    {
        var sb = new StringBuilder();

        sb.AppendLine("\n==== REPLAY START ====");

        foreach (var transition in transitions)
        {
            sb.AppendLine();
            sb.AppendLine($"[{transition.Status}] {transition.Input}");

            var beforeVersion = transition.Before?.Version.ToString() ?? "?";
            var afterVersion = transition.After?.Version.ToString() ?? "not applied";

            sb.AppendLine($"State: {beforeVersion} -> {afterVersion}");

            if (!string.IsNullOrWhiteSpace(transition.Reason))
            {
                sb.AppendLine($"Reason: {transition.Reason}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("==== REPLAY END ====");

        return sb.ToString();
    }

    public static string FormatReplayVerbose(IEnumerable<Transition> transitions)
    {
        var sb = new StringBuilder();

        sb.AppendLine("\n==== REPLAY VERBOSE START ====");

        foreach (var transition in transitions)
        {
            sb.AppendLine();
            sb.AppendLine($"Transition={transition.Id}");

            var beforeVersion = transition.Before?.Version.ToString() ?? "?";
            var afterVersion = transition.After?.Version.ToString() ?? "not applied";

            sb.AppendLine($"State = {beforeVersion} -> {afterVersion}");
            sb.AppendLine($"Status={transition.Status}");
            sb.AppendLine($"Input={transition.Input}");

            if (!string.IsNullOrWhiteSpace(transition.Reason))
            {
                sb.AppendLine($"Reason={transition.Reason}");
            }

            foreach (var e in transition.Events)
            {
                sb.Append('[')
                    .Append(e.Stage)
                    .Append("] ")
                    .AppendLine(e.Message);
            }
        }

        sb.AppendLine();
        sb.AppendLine("==== REPLAY VERBOSE END ====");

        return sb.ToString();
    }

    public static string FormatExplain(Transition transition)
    {
        var sb = new StringBuilder();

        sb.AppendLine("\n==== EXPLAIN START ====");
        sb.AppendLine();

        sb.AppendLine($"Input: {transition.Input}");
        sb.AppendLine($"Outcome: {transition.Status}");
        sb.AppendLine();

        AppendSummary(sb, transition);
        AppendPlan(sb, transition);
        AppendGovernance(sb, transition);
        AppendStateImpact(sb, transition);

        sb.AppendLine();
        sb.AppendLine("==== EXPLAIN END ====");

        return sb.ToString();
    }

    private static void AppendSummary(StringBuilder sb, Transition transition)
    {
        var capability = transition.ProposedAction?.Capability ?? "unknown";

        sb.AppendLine("Summary:");

        if (transition.Status == TransitionStatus.Rejected)
        {
            sb.AppendLine(
                $"  The runtime planned to use '{capability}', but the transition was rejected before execution.");

            if (!string.IsNullOrWhiteSpace(transition.Reason))
                sb.AppendLine($"  {transition.Reason}");
        }
        else if (transition.Status == TransitionStatus.Completed)
        {
            sb.AppendLine(
                $"  The runtime used '{capability}' and completed the transition successfully.");
        }
        else
        {
            sb.AppendLine(
                $"  The runtime used '{capability}' and ended with status '{transition.Status}'.");
        }

        sb.AppendLine();
    }

    private static void AppendPlan(StringBuilder sb, Transition transition)
    {
        sb.AppendLine("Plan:");

        if (transition.ProposedAction == null)
        {
            sb.AppendLine("  No action was proposed.");
        }
        else
        {
            sb.AppendLine($"  Capability: {transition.ProposedAction.Capability}");

            if (!string.IsNullOrWhiteSpace(transition.ProposedAction.Input))
                sb.AppendLine($"  Input: {transition.ProposedAction.Input}");
        }

        sb.AppendLine();
    }

    private static void AppendGovernance(StringBuilder sb, Transition transition)
    {
        var pre = transition.Events.Where(e => e.Stage == TransitionEventStage.PreInvariant).ToList();

        var post = transition.Events.Where(e => e.Stage == TransitionEventStage.PostInvariant).ToList();

        if (pre.Count == 0 && post.Count == 0)
            return;

        sb.AppendLine("Governance:");

        if (pre.Count > 0)
        {
            sb.AppendLine("  Pre-Execution:");

            foreach (var e in pre)
                AppendInvariantEvent(sb, e);

            sb.AppendLine();
        }

        if (post.Count > 0)
        {
            sb.AppendLine("  Post-Execution:");

            foreach (var e in post)
                AppendInvariantEvent(sb, e);

            sb.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(transition.Reason))
        {
            sb.AppendLine("Decision:");
            sb.AppendLine($"  {transition.Reason}");
            sb.AppendLine();
        }
    }

    private static void AppendInvariantEvent(StringBuilder sb, TransitionEvent e)
    {
        var invariant = GetMetadataString(e, "Invariant") ?? "UnknownInvariant";
        var reason = GetMetadataString(e, "Reason");

        var passed = e.Metadata.TryGetValue("Passed", out var passedObj) && passedObj is bool b && b;

        sb.AppendLine(passed
            ? $"    [Pass] {invariant}"
            : $"    [Fail] {invariant}");

        if (!passed && !string.IsNullOrWhiteSpace(reason))
            sb.AppendLine($"           {reason}");
    }

    private static void AppendStateImpact(StringBuilder sb, Transition transition)
    {
        sb.AppendLine("State Impact:");

        if (transition.After == null)
        {
            sb.AppendLine("  No state change was applied.");
            return;
        }

        var beforeVersion = transition.Before?.Version.ToString() ?? "?";
        var afterVersion = transition.After.Version.ToString();

        sb.AppendLine($"  Version changed from {beforeVersion} to {afterVersion}.");

        AppendMemoryDiff(sb, transition);

        var selfModifications = transition.Events
            .Where(e => e.Stage == TransitionEventStage.SelfModification)
            .ToList();

        foreach (var e in selfModifications)
        {
            var target = GetMetadataString(e, "Target") ?? "Unknown";
            var operation = GetMetadataString(e, "Operation") ?? "Changed";
            var key = GetMetadataString(e, "Key") ?? "";

            sb.AppendLine($"  Self modification: {target}.{operation} {key}");
        }
    }

    private static string? GetMetadataString(TransitionEvent e, string key)
    {
        return e.Metadata.TryGetValue(key, out var value) ? value?.ToString() : null;
    }

    private static void AppendMemoryDiff(StringBuilder sb, Transition transition)
    {
        if (transition.Before == null || transition.After == null)
            return;

        var before = transition.Before.Memory;
        var after = transition.After.Memory;

        var keys = before.Keys.Union(after.Keys).OrderBy(x => x).ToList();

        var changes = new List<string>();

        foreach (var key in keys)
        {
            var hadBefore = before.TryGetValue(key, out var beforeValue);
            var hasAfter = after.TryGetValue(key, out var afterValue);

            if (!hadBefore && hasAfter)
                changes.Add($"  + {key}={afterValue}");

            else if (hadBefore && !hasAfter)
                changes.Add($"  - {key}");

            else if (!Equals(beforeValue, afterValue))
                changes.Add($"  ~ {key}={beforeValue} -> {afterValue}");
        }

        if (changes.Count == 0) return;

        sb.AppendLine("Memory Changes:");

        foreach (var change in changes)
            sb.AppendLine(change);
    }
}
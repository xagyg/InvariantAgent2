namespace InvariantAgent.Core.Rendering;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InvariantAgent.Core.Model.Control;
using InvariantAgent.Core.Model.Transition;

/// <summary>
/// Renders transitions as human-readable text for the replay and explain capabilities.
/// </summary>
public static class TransitionFormatter
{
    /// <summary>Compact replay: status, input, state version change and rejection reason per transition.</summary>
    /// <param name="transitions">Transitions to render, in order.</param>
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

    /// <summary>Verbose replay: as <see cref="FormatReplay"/> plus the full event trail of each transition.</summary>
    /// <param name="transitions">Transitions to render, in order.</param>
    public static string FormatReplayVerbose(IEnumerable<Transition> transitions)
    {
        var sb = new StringBuilder();

        sb.AppendLine("\n==== REPLAY VERBOSE START ====");

        foreach (var transition in transitions)
        {
            sb.AppendLine();
            sb.AppendLine($"Transition Id={transition.Id}");

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

    /// <summary>
    /// Explains a single transition: summary, plan, invariant results grouped by scope, and state impact
    /// (version change, memory diff and self-modifications).
    /// </summary>
    /// <param name="transition">The transition to explain.</param>
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

    // One-paragraph narrative of what the runtime did and how the transition ended.
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

    // The planner's proposed capability and input.
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

    // Invariant results for each lifecycle checkpoint, in pipeline order.
    private static void AppendGovernance(StringBuilder sb, Transition transition)
    {
        AppendInvariantEvents(sb, transition, InvariantScope.Plan, "Plan invariants");

        AppendInvariantEvents(sb, transition, InvariantScope.Execution, "Execution invariants");

        AppendInvariantEvents(sb, transition, InvariantScope.SelfModification, "Self-modification invariants");

        AppendInvariantEvents(sb, transition, InvariantScope.Reduction, "Reduction invariants");
    }

    // Lists Invariant-stage events whose "Scope" metadata matches the requested scope.
    private static void AppendInvariantEvents(StringBuilder sb, Transition transition, InvariantScope scope, string title)
    {
        var events = transition.Events
            .Where(e => e.Stage == TransitionEventStage.Invariant && HasScope(e, scope))
            .ToList();

        if (events.Count == 0)
        {
            return;
        }

        sb.AppendLine(title + ":");

        foreach (var e in events)
        {
            sb.AppendLine($"  - {e.Message}");
        }
    }

    // True if the event's "Scope" metadata equals the given scope (case-insensitive).
    private static bool HasScope(TransitionEvent e, InvariantScope scope)
    {
        return e.Metadata != null &&
               e.Metadata.TryGetValue("Scope", out var value) &&
               string.Equals(
                   Convert.ToString(value),
                   scope.ToString(),
                   StringComparison.OrdinalIgnoreCase);
    }


    // Version change, memory diff and any self-modification events.
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

    // Reads a metadata value as a string, or null if absent.
    private static string? GetMetadataString(TransitionEvent e, string key)
    {
        return e.Metadata.TryGetValue(key, out var value) ? value?.ToString() : null;
    }

    // Lists added (+), removed (-) and changed (~) memory keys between Before and After.
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

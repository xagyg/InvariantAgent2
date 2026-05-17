using InvariantAgent.Core.Model.Agent;
using InvariantAgent.Core.Model.Control;
using InvariantAgent.Core.Model.Drift;
using InvariantAgent.Core.Model.Transition;
using System;
using System.Collections.Generic;
using System.Linq;

namespace InvariantAgent.Core.Drift;

public sealed class BehaviouralDriftDetector
{
    public const int DefaultHorizon = 3;
    public const int ReportingThreshold = 25;
    public const int DefaultBoundThreshold = 70;

    private static readonly HashSet<string> VolatileMemoryKeys =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "lastOutcome",
            "last_action"
        };

    public IReadOnlyList<DriftRecord> Detect(IReadOnlyList<Transition> transitions)
    {
        var records = new List<DriftRecord>();

        for (var index = 0; index < transitions.Count; index++)
        {
            var transition = transitions[index];

            if (transition.After == null ||
                transition.Status != TransitionStatus.Completed)
            {
                continue;
            }

            var baseline = GetBaseline(transitions, index);

            var score = Score(baseline, transition.After);

            if (score.Score < ReportingThreshold)
            {
                continue;
            }

            records.Add(
                new DriftRecord
                {
                    Type = DriftType.BehaviouralDrift,
                    Reason = score.Explanation,
                    TransitionId = transition.Id.ToString(),
                    TimestampUtc = transition.Timestamp,
                    Phase = transition.Phase,
                    Severity = score.Severity,
                    Score = score.Score
                });
        }

        return records;
    }

    public DriftScore Score(Transition transition)
    {
        if (transition.Before == null || transition.After == null)
        {
            return new DriftScore
            {
                Score = 0,
                Severity = InvariantSeverity.Info,
                Explanation = "Transition does not contain before and after adaptive state."
            };
        }

        return Score(transition.Before, transition.After);
    }

    public DriftScore Score(AgentState baseline, AgentState current)
    {
        var baselineSnapshot = AdaptiveStateSnapshot.From(baseline);
        var currentSnapshot = AdaptiveStateSnapshot.From(current);
        var comparison = Compare(baselineSnapshot, currentSnapshot);

        return new DriftScore
        {
            Score = comparison.Score,
            Severity = ToSeverity(comparison.Score),
            Explanation = comparison.Reason
        };
    }

    private static AgentState GetBaseline(IReadOnlyList<Transition> transitions, int index)
    {
        var baselineIndex = index - DefaultHorizon;

        if (baselineIndex >= 0)
        {
            return transitions[baselineIndex].After ?? transitions[baselineIndex].Before;
        }

        return transitions[index].Before;
    }

    private static BehaviouralDriftComparison Compare(
        AdaptiveStateSnapshot baseline,
        AdaptiveStateSnapshot current)
    {
        var score = 0;
        var reasons = new List<string>();

        if (!string.Equals(baseline.Goal, current.Goal, StringComparison.Ordinal))
        {
            score += 30;
            reasons.Add($"goal changed from '{Display(baseline.Goal)}' to '{Display(current.Goal)}'");
        }

        if (!string.Equals(baseline.Mode, current.Mode, StringComparison.Ordinal))
        {
            score += 20;
            reasons.Add($"mode changed from '{Display(baseline.Mode)}' to '{Display(current.Mode)}'");
        }

        var addedPolicies = current.Policies.Except(baseline.Policies).ToList();
        var removedPolicies = baseline.Policies.Except(current.Policies).ToList();

        if (addedPolicies.Count > 0 || removedPolicies.Count > 0)
        {
            score += Math.Min(30, (addedPolicies.Count + removedPolicies.Count) * 10);
            reasons.Add(
                $"policies changed (+{addedPolicies.Count}/-{removedPolicies.Count})");
        }

        var memoryChanges = CompareMemory(baseline.Memory, current.Memory);

        if (memoryChanges.Score > 0)
        {
            score += memoryChanges.Score;
            reasons.Add(memoryChanges.Reason);
        }

        return new BehaviouralDriftComparison
        {
            Score = score,
            Reason = reasons.Count == 0
                ? "No adaptive state divergence detected."
                : string.Join("; ", reasons)
        };
    }

    private static BehaviouralDriftComparison CompareMemory(
        IReadOnlyDictionary<string, string> baseline,
        IReadOnlyDictionary<string, string> current)
    {
        var added = current.Keys.Except(baseline.Keys, StringComparer.OrdinalIgnoreCase).Count();
        var removed = baseline.Keys.Except(current.Keys, StringComparer.OrdinalIgnoreCase).Count();
        var changed = baseline
            .Where(kvp =>
                current.TryGetValue(kvp.Key, out var currentValue) &&
                !string.Equals(kvp.Value, currentValue, StringComparison.Ordinal))
            .Count();

        var totalChanges = added + removed + changed;

        if (totalChanges == 0)
        {
            return new BehaviouralDriftComparison
            {
                Score = 0,
                Reason = ""
            };
        }

        return new BehaviouralDriftComparison
        {
            Score = Math.Min(40, added * 10 + removed * 10 + changed * 5),
            Reason = $"adaptive memory changed (+{added}/-{removed}/~{changed})"
        };
    }

    private static InvariantSeverity ToSeverity(int score)
    {
        return score >= DefaultBoundThreshold ? InvariantSeverity.Error :
            score >= 40 ? InvariantSeverity.Warning :
            InvariantSeverity.Info;
    }

    private static string Display(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "<empty>" : value;
    }

    private sealed class AdaptiveStateSnapshot
    {
        public string Goal { get; init; } = "";

        public string Mode { get; init; } = "";

        public IReadOnlyList<string> Policies { get; init; } = Array.Empty<string>();

        public IReadOnlyDictionary<string, string> Memory { get; init; }
            = new Dictionary<string, string>();

        public static AdaptiveStateSnapshot From(AgentState state)
        {
            var memory = state.Memory
                .Where(kvp => !VolatileMemoryKeys.Contains(kvp.Key))
                .OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.ToString() ?? "",
                    StringComparer.OrdinalIgnoreCase);

            var memoryGoal = memory.TryGetValue("goal", out var goal) ? goal : "";

            return new AdaptiveStateSnapshot
            {
                Goal = string.IsNullOrWhiteSpace(state.Goal) ? memoryGoal : state.Goal,
                Mode = state.Mode ?? "",
                Policies = state.Policies
                    .OrderBy(policy => policy, StringComparer.OrdinalIgnoreCase)
                    .ToList(),
                Memory = memory
            };
        }
    }

    private sealed class BehaviouralDriftComparison
    {
        public int Score { get; init; }

        public string Reason { get; init; } = "";
    }
}

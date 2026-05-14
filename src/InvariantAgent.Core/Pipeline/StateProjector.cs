using InvariantAgent.Core.Model.Agent;
using System.Collections.Generic;
using System.Linq;

namespace InvariantAgent.Core.Pipeline
{
    public static class StateProjector
    {
        public static StateProjection Project(AgentState state)
        {
            return new StateProjection
            {
                Version = state.Version,
                Goal = state.Goal ?? "",
                MemorySummary = SummariseMemory(state.Memory),
                LastOutcome = GetLastOutcome(state),
                ActivePolicies = state.Policies?.ToList() ?? new List<string>()
            };
        }

        private static string GetLastOutcome(AgentState state)
        {
            if (state.Memory.TryGetValue("lastOutcome", out var value))
            {
                return value?.ToString() ?? "";
            }

            return "";
        }

        private static string SummariseMemory(Dictionary<string, object> memory)
        {
            if (memory == null || memory.Count == 0)
                return "";

            var importantKeys = new HashSet<string>
            {
                "goal",
                "user_intent",
                "last_action",
                "lastOutcome",
                "error_state"
            };

            var prioritized = memory.Where(kv => importantKeys.Contains(kv.Key));

            return string.Join("; ",
                prioritized
                    .Take(10)
                    .Select(kv => $"{kv.Key}={kv.Value}"));
        }
    }
}
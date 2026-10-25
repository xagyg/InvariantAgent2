using InvariantAgent.Core.Model.Agent;
using System.Collections.Generic;
using System.Linq;

namespace InvariantAgent.Core.Pipeline
{
    /// <summary>
    /// Produces the bounded <see cref="StateProjection"/> <c>π(Sₜ)</c> that planners operate on.
    /// </summary>
    public static class StateProjector
    {
        /// <summary>Creates a projection of the given state.</summary>
        /// <param name="state">The full agent state.</param>
        /// <returns>A read-only, summarised view of the state.</returns>
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

        // Returns the "lastOutcome" memory entry, or an empty string.
        private static string GetLastOutcome(AgentState state)
        {
            if (state.Memory.TryGetValue("lastOutcome", out var value))
            {
                return value?.ToString() ?? "";
            }

            return "";
        }

        // Summarises only an allow-list of memory keys (max 10) so planners cannot see arbitrary memory.
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
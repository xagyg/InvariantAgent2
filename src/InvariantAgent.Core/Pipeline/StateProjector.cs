using InvariantAgent.Core.Model;
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
                Mode = state.Mode,
                ActivePolicies = state.Policies.ToList(),

                MemorySummary = SummariseMemory(state.Memory),

                LastOutcomeSummary = state.Events.OfType<ExecutionEvent>().LastOrDefault()?.Result,

                GoalContext = state.Goal
            };
        }

        private static string SummariseMemory(Dictionary<string, object> memory)
        {
            var importantKeys = new HashSet<string>
                {
                    "goal",
                    "user_intent",
                    "last_action",
                    "error_state"
                };

            var prioritized = memory
                .Where(kv => importantKeys.Contains(kv.Key))
                .OrderByDescending(kv => importantKeys.Contains(kv.Key));

            return string.Join("; ",
                prioritized
                    .Take(10)
                    .Select(kv => $"{kv.Key}={kv.Value}"));
        }
    }
}

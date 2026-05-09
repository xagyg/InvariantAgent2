using System.Collections.Generic;

namespace InvariantAgent.Core.Model
{
    public class StateProjection
    {
        public string Mode { get; set; }

        public IReadOnlyList<string> ActivePolicies { get; set; }

        public string MemorySummary { get; init; }

        public string LastOutcomeSummary { get; init; }

        public string GoalContext { get; init; }
    }
}

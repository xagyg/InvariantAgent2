using System.Collections.Generic;

namespace InvariantAgent.Core.Model.Agent
{
    public sealed class StateProjection
    {
        public int Version { get; init; }

        public string Goal { get; init; } = "";

        public string MemorySummary { get; init; } = "";

        public string LastOutcome { get; init; } = "";

        public IReadOnlyList<string> ActivePolicies { get; init; } = new List<string>();
    }
}
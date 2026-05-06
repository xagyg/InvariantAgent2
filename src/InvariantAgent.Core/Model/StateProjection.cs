using System.Collections.Generic;

namespace InvariantAgent.Core.Model
{
    public class StateProjection
    {
        public string Mode { get; init; }
        public IReadOnlyList<string> ActivePolicies { get; init; }
        public IReadOnlyDictionary<string, object> MemorySnapshot { get; init; }
    }
}

using System.Collections.Generic;

namespace InvariantAgent.Core.Model.Drift
{
    public sealed class DriftReport
    {
        public int TransitionCount { get; init; }

        public int RejectedTransitions { get; init; }

        public IReadOnlyDictionary<string, int> CapabilityUsage { get; init; }
            = new Dictionary<string, int>();

        public IReadOnlyDictionary<string, int> InvariantFailures { get; init; }
            = new Dictionary<string, int>();
    }
}
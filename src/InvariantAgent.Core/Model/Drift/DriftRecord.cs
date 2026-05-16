using InvariantAgent.Core.Model.Control;
using InvariantAgent.Core.Model.Transition;
using System;

namespace InvariantAgent.Core.Model.Drift
{
    public sealed class DriftRecord
    {
        public DriftType Type { get; init; }

        public string Reason { get; init; } = "";

        public string TransitionId { get; init; } = "";

        public DateTime TimestampUtc { get; init; }

        public TransitionPhase Phase { get; init; }

        public InvariantSeverity Severity { get; init; }
    }
}
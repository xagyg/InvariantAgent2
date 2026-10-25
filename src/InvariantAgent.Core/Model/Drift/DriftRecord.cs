using InvariantAgent.Core.Model.Control;
using InvariantAgent.Core.Model.Transition;
using System;

namespace InvariantAgent.Core.Model.Drift
{
    /// <summary>A single observed drift event.</summary>
    public sealed class DriftRecord
    {
        /// <summary>Kind of drift observed.</summary>
        public DriftType Type { get; init; }

        /// <summary>Explanation of what changed.</summary>
        public string Reason { get; init; } = "";

        /// <summary>Id of the transition in which drift was observed.</summary>
        public string TransitionId { get; init; } = "";

        /// <summary>UTC time of the observation.</summary>
        public DateTime TimestampUtc { get; init; }

        /// <summary>Lifecycle phase of the transition when drift was observed.</summary>
        public TransitionPhase Phase { get; init; }

        /// <summary>Severity band of the drift.</summary>
        public InvariantSeverity Severity { get; init; }

        /// <summary>Numeric drift score.</summary>
        public int Score { get; init; }
    }
}
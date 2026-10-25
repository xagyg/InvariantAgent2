using InvariantAgent.Core.Model.Agent;
using System;

namespace InvariantAgent.Core.Model.Drift
{
    /// <summary>An operator-approved reference state used for drift scoring.</summary>
    public sealed class DriftBaseline
    {
        /// <summary>Baseline identifier.</summary>
        public Guid Id { get; init; } = Guid.NewGuid();

        /// <summary>UTC time of approval.</summary>
        public DateTime ApprovedAtUtc { get; init; } = DateTime.UtcNow;

        /// <summary>Version of the approved state.</summary>
        public int StateVersion { get; init; }

        /// <summary>Justification recorded at approval.</summary>
        public string Reason { get; init; } = "";

        /// <summary>Snapshot of the approved state.</summary>
        public AgentState State { get; init; } = new();
    }
}

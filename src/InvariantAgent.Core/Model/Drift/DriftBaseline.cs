using InvariantAgent.Core.Model.Agent;
using System;

namespace InvariantAgent.Core.Model.Drift
{
    public sealed class DriftBaseline
    {
        public Guid Id { get; init; } = Guid.NewGuid();

        public DateTime ApprovedAtUtc { get; init; } = DateTime.UtcNow;

        public int StateVersion { get; init; }

        public string Reason { get; init; } = "";

        public AgentState State { get; init; } = new();
    }
}

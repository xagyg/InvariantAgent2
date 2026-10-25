using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Agent;
using InvariantAgent.Core.Model.Drift;
using System.Collections.Generic;
using System.Linq;

namespace InvariantAgent.Core.Drift
{
    /// <summary>
    /// Process-local <see cref="IDriftBaselineStore"/>. The approved baseline is lost when the process exits.
    /// </summary>
    public sealed class InMemoryDriftBaselineStore : IDriftBaselineStore
    {
        private DriftBaseline _current;

        /// <inheritdoc />
        public DriftBaseline Current => _current;

        /// <inheritdoc />
        public DriftBaseline Approve(AgentState state, string reason)
        {
            _current = new DriftBaseline
            {
                StateVersion = state.Version,
                Reason = reason,
                State = CloneState(state)
            };

            return _current;
        }

        // Defensive copy so later changes to the live state cannot alter the approved baseline.
        // Memory values are copied by reference (shallow copy).
        private static AgentState CloneState(AgentState state)
        {
            return new AgentState
            {
                Version = state.Version,
                Mode = state.Mode,
                Goal = state.Goal,
                Policies = state.Policies.ToList(),
                Events = state.Events.ToList(),
                Memory = new Dictionary<string, object>(state.Memory)
            };
        }
    }
}

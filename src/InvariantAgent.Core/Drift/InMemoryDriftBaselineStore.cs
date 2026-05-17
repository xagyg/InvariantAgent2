using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Agent;
using InvariantAgent.Core.Model.Drift;
using System.Collections.Generic;
using System.Linq;

namespace InvariantAgent.Core.Drift
{
    public sealed class InMemoryDriftBaselineStore : IDriftBaselineStore
    {
        private DriftBaseline _current;

        public DriftBaseline Current => _current;

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

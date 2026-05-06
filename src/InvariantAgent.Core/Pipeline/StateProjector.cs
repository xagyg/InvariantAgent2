using InvariantAgent.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvariantAgent.Core.Pipeline
{
    public static class StateProjector
    {
        public static StateProjection Project(AgentState state)
        {
            return new StateProjection
            {
                Mode = state.Mode,
                ActivePolicies = state.Policies.ToList(),
                MemorySnapshot = new Dictionary<string, object>(state.Memory)
            };
        }
    }
}

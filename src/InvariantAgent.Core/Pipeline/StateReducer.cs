using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model;
using System.Collections.Generic;

namespace InvariantAgent.Core.Pipeline
{
    public class StateReducer : IStateReducer
    {
        public AgentState Reduce(AgentState state, AgentOutcome outcome)
        {
            state.Memory["lastOutcome"] = outcome.Result;
            state.Version++;

            return state;
        }
    }
}

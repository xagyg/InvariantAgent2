using InvariantAgent.Core.Model;
using System.Collections.Generic;

namespace InvariantAgent.Core.Pipeline
{
    public class StateReducer
    {
        public AgentState Reduce(AgentState state, AgentOutcome outcome)
        {
            state.Memory["lastOutcome"] = outcome.Result;
            state.Version++;

            return state;
        }

        public List<AgentEvent> Apply(List<AgentEvent> history, AgentOutcome outcome)
        {
            history.Add(new AgentEvent
            {
                Type = "OutcomeAccepted",
                Payload = outcome
            });

            return history;
        }
    }
}

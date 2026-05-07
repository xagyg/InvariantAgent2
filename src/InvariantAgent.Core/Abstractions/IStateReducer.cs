using InvariantAgent.Core.Events;
using InvariantAgent.Core.Model;
using System.Collections.Generic;

namespace InvariantAgent.Core.Abstractions
{
    public interface IStateReducer
    {
        StateProjection Reduce(IEnumerable<AgentEvent> events);
    }
}

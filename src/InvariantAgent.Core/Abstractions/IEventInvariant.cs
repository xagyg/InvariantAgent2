using InvariantAgent.Core.Events;
using InvariantAgent.Core.Model;

namespace InvariantAgent.Core.Abstractions
{
    public interface IEventInvariant : IInvariant<AgentEvent>
    {
    }
}

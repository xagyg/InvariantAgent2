
namespace InvariantAgent.Observability
{
    public class EventStore
    {
        private readonly List<AgentEvent> _events = new();

        public void Append(AgentEvent evt)
        {
            _events.Add(evt);
        }

        public IReadOnlyList<AgentEvent> GetAll()
            => _events;
    }
}

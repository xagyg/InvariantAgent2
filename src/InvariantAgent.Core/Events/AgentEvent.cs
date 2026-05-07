using System;

namespace InvariantAgent.Core.Events
{
    public class AgentEvent
    {
        public Guid Id { get; init; } = Guid.NewGuid();

        public DateTime Timestamp { get; init; } = DateTime.UtcNow;

        public string Type { get; init; } = "";

        public object Payload { get; init; }       
    }
}

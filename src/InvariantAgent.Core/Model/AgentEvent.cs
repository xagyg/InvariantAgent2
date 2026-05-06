using System;

namespace InvariantAgent.Core.Model
{
    public class AgentEvent
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public string Type { get; set; }
        public object Payload { get; set; }
    }
}

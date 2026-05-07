using System;
using System.Collections.Generic;
using InvariantAgent.Core.Events;

namespace InvariantAgent.Core.Model
{
    public class AgentState
    {
        public int Version { get; set; } = 0;
        public Dictionary<string, object> Memory { get; set; } = new();
        public List<AgentEvent> Events { get; set; } = new();
        public string Mode { get; set; } = "";
        public List<string> Policies { get; set; } = new();

        public void AddEvent(string type, string payload)
        {
            Events.Add(new AgentEvent
            {
                Type = type,
                Payload = payload,
                Timestamp = DateTime.UtcNow
            });
        }
    }
}

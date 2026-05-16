using System.Collections.Generic;

namespace InvariantAgent.Core.Model.Agent
{
    public class AgentState
    {
        public int Version { get; set; } = 0;
        public Dictionary<string, object> Memory { get; set; } = new();
        public List<AgentEvent> Events { get; set; } = new();
        public string Mode { get; set; } = "";
        public List<string> Policies { get; set; } = new();

        public string Goal { get; set; }

        public void AddEvent(AgentEvent e)
        {
            Events.Add(e);
        }
    }
}

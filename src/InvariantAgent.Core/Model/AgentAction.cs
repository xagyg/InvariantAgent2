using System.Collections.Generic;

namespace InvariantAgent.Core.Model
{
    public class AgentAction
    {
        public string Tool { get; set; }
        public string Input { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
    }
}

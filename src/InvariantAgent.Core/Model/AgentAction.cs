namespace InvariantAgent.Core.Model
{
    public class AgentAction
    {
        public string Capability { get; set; }
        public string Input { get; set; }

        // null = success, non-null = failure reason
        public string? Error { get; set; }

        public bool HasError => !string.IsNullOrEmpty(Error);
    }
}
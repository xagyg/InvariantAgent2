namespace InvariantAgent.Core.Model.Agent
{
    public sealed class AgentOutcome
    {
        public bool Success { get; init; }

        public string Capability { get; init; } = "";

        public string Result { get; init; } = "";

        public string Error { get; init; } = "";
    }
}
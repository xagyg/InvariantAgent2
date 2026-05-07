
namespace InvariantAgent.Core.Events
{
    public sealed class ToolExecutedEvent : AgentEvent
    {
        public string Tool { get; init; }
        public string Result { get; init; }
    }
}

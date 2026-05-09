using InvariantAgent.Core.Model;

namespace InvariantAgent.Core.Abstractions;
public interface ITool
{
    string Name { get; }

    ToolResult Run(string input, AgentState state);
} 
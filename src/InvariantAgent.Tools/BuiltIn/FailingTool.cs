using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model;

namespace InvariantAgent.Tools.BuiltIn;

public class FailingTool : ITool
{
    public string Name => "fail";

    public ToolResult Run(string input, AgentState state)
    {
        throw new InvalidOperationException("Simulated tool failure");
    }
}
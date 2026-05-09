using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model;

using InvariantAgent.Core.Model.ToolData;

namespace InvariantAgent.Tools.BuiltIn;

public class EchoTool : ITool
{
    public string Name => "echo";

    public ToolResult Run(string input, AgentState state)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return ToolResult.Fail(Name, "Input was empty");
        }

        return ToolResult.Ok(
            Name,
            new TextData { Value = input }
        );
    }
}
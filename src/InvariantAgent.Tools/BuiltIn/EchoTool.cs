using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model;

namespace InvariantAgent.Tools.BuiltIn;

public class EchoTool : ITool
{
    public string Name => "echo";

    public ToolResult Run(string input, AgentState state)
    {
        var trimmedInput = input;

        if (input.StartsWith(Name + " ", StringComparison.OrdinalIgnoreCase))
        {
            trimmedInput = input.Substring(Name.Length + 1);
        }

        Console.WriteLine(trimmedInput);

        return ToolResult.Ok(Name, input);
    }
}
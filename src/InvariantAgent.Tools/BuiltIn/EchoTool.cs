using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model;

namespace InvariantAgent.Tools.BuiltIn;

public class EchoTool : ITool
{
    public string Name => "echo";

    public object Run(string input, AgentState state)
    {
        return input;
    }
}
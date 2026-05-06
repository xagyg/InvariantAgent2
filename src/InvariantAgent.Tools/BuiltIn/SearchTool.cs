using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model;

namespace InvariantAgent.Tools.BuiltIn;

public class SearchTool : ITool
{
    public string Name => "search";

    public object Run(string input, AgentState state)
    {
        return new
        {
            Query = input,
            Results = new[]
            {
                $"Result 1 for '{input}'",
                $"Result 2 for '{input}'"
            }
        };
    }
}
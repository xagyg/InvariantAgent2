using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model;
using InvariantAgent.Core.Model.ToolData;

namespace InvariantAgent.Tools.BuiltIn;

public class SearchTool : ITool
{
    public string Name => "search";

    public ToolResult Run(string input, AgentState state)
    {
        return ToolResult.Ok(Name, new ListData {                    
            Rows = new List<string>
                {
                $"Result 1 for '{input}'",
                $"Result 2 for '{input}'"
                }            
        });        
    }
}
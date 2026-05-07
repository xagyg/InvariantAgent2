using System.Data;
using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model;

namespace InvariantAgent.Tools.BuiltIn;

public class CalculatorTool : ITool
{
    public string Name => "calculator";

    public ToolResult Run(string input, AgentState state)
    {
        var table = new DataTable();
        var result = table.Compute(input, string.Empty);

        return ToolResult.Ok(Name,  result);
    }
}
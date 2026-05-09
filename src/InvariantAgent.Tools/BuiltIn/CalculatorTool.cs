using System.Data;
using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model;
using InvariantAgent.Core.Model.ToolData;

namespace InvariantAgent.Tools.BuiltIn;

public class CalculatorTool : ITool
{
    public string Name => "calc";

    public ToolResult Run(string input, AgentState state)
    {
        var table = new DataTable();
        var result = table.Compute(input, string.Empty);

        return ToolResult.Ok(Name, new TextData
        {
            Value = result?.ToString() ?? ""
        });
    }
}
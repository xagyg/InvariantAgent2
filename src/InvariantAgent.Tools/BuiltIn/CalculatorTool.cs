using System.Data;
using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model;

namespace InvariantAgent.Tools.BuiltIn;

public class CalculatorTool : ITool
{
    public string Name => "calculator";

    public object Run(string input, AgentState state)
    {
        try
        {
            var table = new DataTable();
            var result = table.Compute(input, string.Empty);

            return result;
        }
        catch
        {
            return $"Invalid expression: {input}";
        }
    }
}
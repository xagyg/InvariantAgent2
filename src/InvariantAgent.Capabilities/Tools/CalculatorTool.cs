using System.Data;
using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model;
using InvariantAgent.Core.Model.Data;

namespace InvariantAgent.Capabilities.Tools;

public class CalculatorTool : ICapability
{
    public string Name => "calc";

    public CapabilityResult Execute(CapabilityRequest request, AgentState state)
    {
        var table = new DataTable();
        var result = table.Compute(request.Input, string.Empty);

        return CapabilityResult.Ok(Name, new TextData
        {
            Value = result?.ToString() ?? ""
        });
    }
}
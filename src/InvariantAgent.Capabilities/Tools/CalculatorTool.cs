using System.Data;
using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Capability;
using InvariantAgent.Core.Model.Data;

namespace InvariantAgent.Capabilities.Tools;

/// <summary>
/// Evaluates arithmetic expressions (e.g. <c>calc 10+20</c>) using <see cref="DataTable.Compute"/>.
/// </summary>
/// <remarks>
/// Invalid expressions throw. <c>CapabilityExecutor</c> converts the exception into a failed result,
/// which execution invariants then reject.
/// </remarks>
public class CalculatorTool : ICapability
{
    /// <inheritdoc />
    public string Name => "calc";

    /// <inheritdoc />
    public CapabilityResult Execute(CapabilityRequest request)
    {
        var table = new DataTable();
        var result = table.Compute(request.Input, string.Empty);

        return CapabilityResult.Ok(Name, new TextData
        {
            Value = result?.ToString() ?? ""
        });
    }
}
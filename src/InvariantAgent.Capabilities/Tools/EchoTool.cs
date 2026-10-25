using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Capability;
using InvariantAgent.Core.Model.Data;

namespace InvariantAgent.Capabilities.Tools;

/// <summary>Returns its input unchanged; fails on empty input.</summary>
public class EchoTool : ICapability
{
    /// <inheritdoc />
    public string Name => "echo";

    /// <inheritdoc />
    public CapabilityResult Execute(CapabilityRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Input))
        {
            return CapabilityResult.Fail(Name, "Input was empty");
        }

        return CapabilityResult.Ok(
            Name,
            new TextData { Value = request.Input }
        );
    }
}
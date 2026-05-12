using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Capability;
using InvariantAgent.Core.Model.Data;

namespace InvariantAgent.Capabilities.Tools;

public class EchoTool : ICapability
{
    public string Name => "echo";

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
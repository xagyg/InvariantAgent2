using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model;
using InvariantAgent.Core.Model.Data;

namespace InvariantAgent.Capabilities.Tools;

public class EchoTool : ICapability
{
    public string Name => "echo";

    public CapabilityResult Execute(CapabilityRequest request, AgentState state)
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
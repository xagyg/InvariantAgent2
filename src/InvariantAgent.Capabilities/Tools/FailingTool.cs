using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Capability;

namespace InvariantAgent.Capabilities.Tools;

public class FailingTool : ICapability
{
    public string Name => "fail";

    public CapabilityResult Execute(CapabilityRequest request)
    {
        throw new InvalidOperationException("Simulated tool failure");
    }
}
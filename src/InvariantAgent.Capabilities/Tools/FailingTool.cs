using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model;

namespace InvariantAgent.Capabilities.Tools;

public class FailingTool : ICapability
{
    public string Name => "fail";

    public CapabilityResult Execute(CapabilityRequest request, AgentState state)
    {
        throw new InvalidOperationException("Simulated tool failure");
    }
}
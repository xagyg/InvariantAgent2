using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Capability;

namespace InvariantAgent.Capabilities.Tools;

/// <summary>
/// Test capability that always throws, for exercising failure handling. Not registered by default.
/// </summary>
public class FailingTool : ICapability
{
    /// <inheritdoc />
    public string Name => "fail";

    /// <inheritdoc />
    public CapabilityResult Execute(CapabilityRequest request)
    {
        throw new InvalidOperationException("Simulated tool failure");
    }
}
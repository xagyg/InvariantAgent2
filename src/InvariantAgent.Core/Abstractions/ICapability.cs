using InvariantAgent.Core.Model.Capability;

namespace InvariantAgent.Core.Abstractions;
public interface ICapability
{
    string Name { get; }

    CapabilityResult Execute(CapabilityRequest request);
} 
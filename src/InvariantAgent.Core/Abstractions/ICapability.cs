using InvariantAgent.Core.Model;

namespace InvariantAgent.Core.Abstractions;
public interface ICapability
{
    string Name { get; }

    CapabilityResult Execute(CapabilityRequest request, AgentState state);
} 
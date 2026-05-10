
namespace InvariantAgent.Core.Abstractions;

public interface ICapabilityRegistry
{
    ICapability Get(string capabilityName);
}
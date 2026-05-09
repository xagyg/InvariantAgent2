
namespace InvariantAgent.Core.Abstractions;

public interface ICapabiltyRegistry
{
    ICapability Get(string capabilityName);
}
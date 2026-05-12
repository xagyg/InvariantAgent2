using InvariantAgent.Core.Abstractions;

namespace InvariantAgent.Capabilities;

public class CapabilityRegistry : ICapabilityRegistry
{
    private readonly Dictionary<string, ICapability> _capabilities;

    public CapabilityRegistry(IEnumerable<ICapability> capabilities)
    {
        _capabilities = capabilities.ToDictionary(c => c.Name);
    }

    public ICapability Get(string name)
    {
        if (_capabilities.TryGetValue(name, out var capability))
            return capability;

        return null;
    }

    public IReadOnlyCollection<string> GetCapabilityNames()
    {
        return _capabilities.Select(c => c.Key).ToList();
    }
}
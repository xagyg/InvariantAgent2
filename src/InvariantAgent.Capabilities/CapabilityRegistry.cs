using InvariantAgent.Core.Abstractions;

namespace InvariantAgent.Capabilities;

/// <summary>
/// Default <see cref="ICapabilityRegistry"/>: indexes capabilities by name and aliases (case-insensitive).
/// </summary>
public class CapabilityRegistry : ICapabilityRegistry
{
    // Name/alias to capability map. Later registrations win if names collide.
    private readonly Dictionary<string, ICapability> _capabilities;

    /// <summary>Builds the registry from all registered capabilities.</summary>
    /// <param name="capabilities">Capabilities supplied by dependency injection.</param>
    public CapabilityRegistry(IEnumerable<ICapability> capabilities)
    {
        _capabilities = new Dictionary<string, ICapability>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var capability in capabilities)
        {
            // primary name
            _capabilities[capability.Name] = capability;

            // aliases
            foreach (var alias in capability.Aliases)
            {
                _capabilities[alias] = capability;
            }
        }
    }

    /// <inheritdoc />
    public ICapability Get(string name)
    {
        if (_capabilities.TryGetValue(name, out var capability))
            return capability;

        return null;
    }

    /// <inheritdoc />
    public IReadOnlyCollection<string> GetCapabilityNames()
    {
        return _capabilities.Select(c => c.Key).ToList();
    }
}
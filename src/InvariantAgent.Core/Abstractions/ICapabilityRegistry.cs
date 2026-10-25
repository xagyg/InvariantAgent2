
using System.Collections.Generic;

namespace InvariantAgent.Core.Abstractions;

/// <summary>
/// Resolves capability names (and aliases) to <see cref="ICapability"/> instances.
/// </summary>
public interface ICapabilityRegistry
{
    /// <summary>Looks up a capability by name or alias.</summary>
    /// <param name="capabilityName">The capability name or alias requested by the planner.</param>
    /// <returns>The matching capability, or <c>null</c> if none is registered.</returns>
    ICapability Get(string capabilityName);

    /// <summary>Returns every registered name and alias. Used to seed the allowed-capability invariant.</summary>
    IReadOnlyCollection<string> GetCapabilityNames();
}
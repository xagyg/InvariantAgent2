
using System.Collections.Generic;

namespace InvariantAgent.Core.Abstractions;

public interface ICapabilityRegistry
{
    ICapability Get(string capabilityName);

    IReadOnlyCollection<string> GetCapabilityNames();
}
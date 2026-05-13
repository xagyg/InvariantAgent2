using InvariantAgent.Core.Model.Capability;
using System.Collections.Generic;
using System;

namespace InvariantAgent.Core.Abstractions;
public interface ICapability
{
    string Name { get; }

    IReadOnlyCollection<string> Aliases => Array.Empty<string>();

    CapabilityResult Execute(CapabilityRequest request);
} 
using InvariantAgent.Core.Model.Capability;
using System.Collections.Generic;
using System;

namespace InvariantAgent.Core.Abstractions;
/// <summary>
/// A unit of execution (tool or service) that the runtime can invoke on behalf of the agent.
/// </summary>
/// <remarks>
/// In IBAAM terms a capability belongs to the Execution layer. Capabilities never mutate
/// runtime state directly: they return a <see cref="CapabilityResult"/> which may carry a
/// proposed self-modification that is only applied after invariant approval and reduction.
/// </remarks>
public interface ICapability
{
    /// <summary>Primary name used by planners and the capability registry to select this capability.</summary>
    string Name { get; }

    /// <summary>Optional alternative names that also resolve to this capability. Empty by default.</summary>
    IReadOnlyCollection<string> Aliases => Array.Empty<string>();

    /// <summary>Executes the capability with the supplied request.</summary>
    /// <param name="request">The input selected by the planner.</param>
    /// <returns>The execution result, including any proposed (not yet applied) self-modification.</returns>
    CapabilityResult Execute(CapabilityRequest request);
} 
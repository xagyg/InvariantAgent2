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

    /// <summary>
    /// <c>true</c> for governance capabilities only a human operator may invoke (resolving conscience escalations,
    /// re-certifying the drift baseline, changing the operational context). Defaults to <c>false</c>.
    /// </summary>
    /// <remarks>
    /// Enforced by <c>OperatorOnlyCapabilityInvariant</c>, not by planner prompts: an operator-only capability runs only
    /// when the operator typed that exact command, so a planner (an LLM in particular) can never propose one itself.
    /// Otherwise the agent could clear its own escalations or move the baseline its drift is measured against, and the
    /// control layer would no longer be outside the agent's reach (IBAAM 1, Section 4.3).
    /// </remarks>
    bool OperatorOnly => false;

    /// <summary>Executes the capability with the supplied request.</summary>
    /// <param name="request">The input selected by the planner.</param>
    /// <returns>The execution result, including any proposed (not yet applied) self-modification.</returns>
    CapabilityResult Execute(CapabilityRequest request);
} 
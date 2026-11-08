using InvariantAgent.Core.Model.Agent;
using InvariantAgent.Core.Model.Control;
using InvariantAgent.Core.Model.Transition;
using System;
using System.Collections.Generic;

namespace InvariantAgent.Core.Abstractions
{
    /// <summary>
    /// A pre-authorised protocol: a rule, approved in advance by the responsible governance body, that authorises a
    /// governed action which violates a fundamental invariant (IBAAM 3 revised, Section 7.2).
    /// </summary>
    /// <remarks>
    /// Without a protocol, an action that violates a fundamental invariant is never carried out autonomously: the agent
    /// holds at the null action and the decision is escalated for human review. Protocols are governance artefacts:
    /// register them in the composition root, never let the agent create or change them.
    /// </remarks>
    public interface IEscalationProtocol
    {
        /// <summary>Protocol name recorded on the decision.</summary>
        string Name { get; }

        /// <summary>Returns <c>true</c> if this protocol covers the case and authorises the action.</summary>
        bool Authorises(EscalationRequest request);
    }

    /// <summary>A governed action that needs fundamental-level authorisation.</summary>
    public sealed class EscalationRequest
    {
        /// <summary>The transition being governed.</summary>
        public TransitionContext Context { get; init; } = default!;

        /// <summary>The checkpoint.</summary>
        public InvariantScope Scope { get; init; }

        /// <summary>The governed action that would be carried out (<c>null</c> if it is the decline option).</summary>
        public AgentAction Action { get; init; }

        /// <summary>The fundamental invariants the action violates.</summary>
        public IReadOnlyList<InvariantViolation> FundamentalViolations { get; init; } = Array.Empty<InvariantViolation>();

        /// <summary>The action's violation profile.</summary>
        public ViolationProfile Profile { get; init; } = ViolationProfile.Zero;
    }
}

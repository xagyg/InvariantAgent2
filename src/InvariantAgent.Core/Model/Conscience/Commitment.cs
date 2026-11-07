using System;

namespace InvariantAgent.Core.Model.Conscience
{
    /// <summary>The scope a commitment applies to (IBAAM 6, Section 6.4.2).</summary>
    /// <remarks>Numeric values are persisted, so new members must only ever be appended.</remarks>
    public enum CommitmentScope
    {
        /// <summary>Only the runtime session (conversation) in which it arose.</summary>
        Conversation,
        /// <summary>Every session of the same user.</summary>
        User,
        /// <summary>The whole deployment.</summary>
        Deployment
    }

    /// <summary>Who made a commitment. Reviewer commitments take precedence over the agent's own.</summary>
    public enum CommitmentOrigin
    {
        /// <summary>Created by the conscience when it adopted a Revise, Escalate or Veto stance.</summary>
        Agent,
        /// <summary>Created when a human reviewer resolved an escalation.</summary>
        Reviewer
    }

    /// <summary>Whether the commitment records conduct declined or conduct undertaken (cleared).</summary>
    public enum CommitmentKind
    {
        /// <summary>The agent (or a reviewer) declined this kind of conduct. Similar plans raise commitment conflict.</summary>
        Declined,
        /// <summary>
        /// A reviewer cleared this kind of conduct. Supersedes earlier declined commitments on the same subject and
        /// lets similar plans proceed to the controls.
        /// </summary>
        Undertaken
    }

    /// <summary>
    /// A record of conduct the agent has declined or undertaken, against which later plans are appraised
    /// (IBAAM 6, Section 6.4.2). This is the operational form of the agent's self-narrative.
    /// </summary>
    /// <remarks>
    /// Commitments live in an <c>ICommitmentStore</c> rather than in <c>AgentState</c>: in IA2 a rejected transition
    /// can never change agent state, yet a Veto or Escalate (which rejects the transition) must still leave a commitment
    /// behind. Like drift baselines, commitments are governance state, persisted beyond any context window.
    /// Commitments about individuals are sensitive data in their own right (Section 9).
    /// </remarks>
    /// <param name="Id">Commitment identifier.</param>
    /// <param name="SourceTransition">The transition where the commitment arose.</param>
    /// <param name="Value">Protected-value tag (same tags as invariants).</param>
    /// <param name="Scope">Where the commitment applies.</param>
    /// <param name="Statement">The conduct declined or undertaken, in words.</param>
    /// <param name="Origin">Agent or reviewer.</param>
    /// <param name="Created">Creation time.</param>
    /// <param name="Expires">Optional expiry; <c>null</c> means it lasts until superseded or its scope ends.</param>
    public sealed record Commitment(
        Guid Id,
        Guid SourceTransition,
        string Value,
        CommitmentScope Scope,
        string Statement,
        CommitmentOrigin Origin,
        DateTimeOffset Created,
        DateTimeOffset? Expires)
    {
        /// <summary>Declined or undertaken.</summary>
        public CommitmentKind Kind { get; init; } = CommitmentKind.Declined;

        /// <summary>Capability of the conduct the commitment is about (used to match later plans).</summary>
        public string Capability { get; init; } = "";

        /// <summary>Normalised input of the conduct the commitment is about (used to match later plans).</summary>
        public string Subject { get; init; } = "";

        /// <summary>Session the commitment arose in (matters for <see cref="CommitmentScope.Conversation"/>).</summary>
        public string ConversationId { get; init; } = "";

        /// <summary>User the commitment arose for (matters for <see cref="CommitmentScope.User"/>).</summary>
        public string UserId { get; init; } = "";

        /// <summary>For a reviewer commitment, the escalated (or vetoed) transition it resolves.</summary>
        public Guid? Resolves { get; init; }
    }
}

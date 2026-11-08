using System;
using System.Collections.Generic;

namespace InvariantAgent.Core.Model.Control
{
    /// <summary>
    /// An auditable record of a violation the governed action carries (IBAAM 3 revised, Section 7).
    /// </summary>
    /// <remarks>
    /// An override exists only when the selected action is in the governed set and every meta-invariant allows the
    /// override. The record carries the witnesses showing that every candidate which would have violated the invariant less
    /// has a worse violation profile, the generated justification and review flags, so the override can be recomputed
    /// and inspected later.
    /// </remarks>
    public sealed class InvariantOverride
    {
        /// <summary>The violation that was overridden.</summary>
        public InvariantViolation OverriddenViolation { get; init; } = new();

        /// <summary>
        /// Candidates that would have violated the invariant less, each with the profile position at which it is worse.
        /// </summary>
        public IReadOnlyList<OverrideWitness> Witnesses { get; init; } = Array.Empty<OverrideWitness>();

        /// <summary>Names of higher-level invariants the governed action satisfies (context for reviewers).</summary>
        public IReadOnlyList<string> PreservedHigherPriorityInvariants { get; init; }
            = Array.Empty<string>();

        /// <summary>Generated rationale explaining why the override was necessary.</summary>
        public string Justification { get; init; } = "";

        /// <summary>Comma-separated list of meta-invariant categories that participated (kept for display/back-compat).</summary>
        public string MetaInvariantCategory { get; init; } = "Priority";

        /// <summary>Distinct meta-invariant categories that evaluated and allowed this override.</summary>
        public IReadOnlyList<MetaInvariantCategory> MetaInvariantCategories { get; init; }
            = Array.Empty<MetaInvariantCategory>();

        /// <summary><c>true</c> when the override situation is ambiguous or noisy and should be reviewed by a human.</summary>
        public bool RequiresReview { get; init; }

        /// <summary>Reasons the override was flagged for review (e.g. same-level conflicts, ties).</summary>
        public IReadOnlyList<string> ReviewReasons { get; init; }
            = Array.Empty<string>();

        /// <summary>
        /// How criticality and context decided the override: <see cref="ContextualGovernanceOutcome.Subordinate"/> when a
        /// same-level comparison decided it, <see cref="ContextualGovernanceOutcome.NotApplicable"/> when priority between
        /// levels decided it.
        /// </summary>
        public ContextualGovernanceDecision ContextualDecision { get; init; }
            = new();

        /// <summary>Pre-authorised protocol that authorised a fundamental-level override, if any.</summary>
        public string EscalationProtocol { get; init; } = "";

        /// <summary>Convenience accessor for the governance level of the overridden invariant.</summary>
        public InvariantLayer OverriddenLayer => OverriddenViolation.Layer;
    }
}

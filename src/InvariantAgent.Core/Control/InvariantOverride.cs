using System;
using System.Collections.Generic;

namespace InvariantAgent.Core.Model.Control
{
    /// <summary>
    /// An auditable record of a violation that was overridden under Hierarchical Invariant Governance.
    /// </summary>
    /// <remarks>
    /// An override is only created when every meta-invariant allows it. The record captures the
    /// preserved higher-priority invariants, the generated justification, review flags and, where
    /// applicable, the HIG-C contextual decision so the override can be inspected later.
    /// </remarks>
    public sealed class InvariantOverride
    {
        /// <summary>The violation that was overridden.</summary>
        public InvariantViolation OverriddenViolation { get; init; } = new();

        /// <summary>Names of higher-layer invariants that passed and therefore justify the override.</summary>
        public IReadOnlyList<string> PreservedHigherPriorityInvariants { get; init; }
            = Array.Empty<string>();

        /// <summary>Generated rationale explaining why the override was permitted.</summary>
        public string Justification { get; init; } = "";

        /// <summary>Comma-separated list of meta-invariant categories that participated (kept for display/back-compat).</summary>
        public string MetaInvariantCategory { get; init; } = "Priority";

        /// <summary>Distinct meta-invariant categories that evaluated and allowed this override.</summary>
        public IReadOnlyList<MetaInvariantCategory> MetaInvariantCategories { get; init; }
            = Array.Empty<MetaInvariantCategory>();

        /// <summary><c>true</c> when the override situation is ambiguous or noisy and should be reviewed by a human.</summary>
        public bool RequiresReview { get; init; }

        /// <summary>Reasons the override was flagged for review (e.g. same-layer conflicts).</summary>
        public IReadOnlyList<string> ReviewReasons { get; init; }
            = Array.Empty<string>();

        /// <summary>The HIG-C (criticality and context) decision made for this violation.</summary>
        public ContextualGovernanceDecision ContextualDecision { get; init; }
            = new();

        /// <summary>Convenience accessor for the governance layer of the overridden invariant.</summary>
        public InvariantLayer OverriddenLayer => OverriddenViolation.Layer;
    }
}

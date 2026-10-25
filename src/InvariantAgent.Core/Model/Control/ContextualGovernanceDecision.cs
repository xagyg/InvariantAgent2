using System;
using System.Collections.Generic;

namespace InvariantAgent.Core.Model.Control
{
    /// <summary>
    /// The HIG-C decision for a violation that conflicts with other violations in the same layer.
    /// </summary>
    /// <remarks>
    /// HIG-C keeps hierarchy as the primary mechanism; criticality and operational context are only
    /// used to distinguish equal-priority (same-layer) conflicts.
    /// </remarks>
    public sealed class ContextualGovernanceDecision
    {
        /// <summary>The contextual resolution outcome.</summary>
        public ContextualGovernanceOutcome Outcome { get; init; }
            = ContextualGovernanceOutcome.NotApplicable;

        /// <summary>Criticality of the violation being decided.</summary>
        public InvariantCriticality Criticality { get; init; }
            = InvariantCriticality.Medium;

        /// <summary>Operational context in effect when the decision was made.</summary>
        public OperationalContext OperationalContext { get; init; }
            = OperationalContext.Normal;

        /// <summary>Contexts in which the violated invariant is especially relevant.</summary>
        public IReadOnlyList<OperationalContext> Contexts { get; init; }
            = Array.Empty<OperationalContext>();

        /// <summary>Names of the same-layer invariants it was compared against.</summary>
        public IReadOnlyList<string> ComparedInvariants { get; init; }
            = Array.Empty<string>();

        /// <summary>Human-readable explanation of the decision.</summary>
        public string Reason { get; init; } = "";
    }
}

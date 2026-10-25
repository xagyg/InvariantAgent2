namespace InvariantAgent.Core.Model.Control
{
    /// <summary>
    /// A failed invariant evaluation, carrying the invariant's governance metadata at the time of failure.
    /// </summary>
    public sealed class InvariantViolation
    {
        /// <summary>Name of the violated invariant.</summary>
        public string Invariant { get; init; } = "";

        /// <summary>Category of the violated invariant.</summary>
        public InvariantCategory Category { get; init; }

        /// <summary>Scope at which the invariant was evaluated.</summary>
        public InvariantScope Scope { get; init; }

        /// <summary>Severity of the violation; Error and Critical are blocking by default.</summary>
        public InvariantSeverity Severity { get; init; }

        /// <summary>HIG layer of the violated invariant.</summary>
        public InvariantLayer Layer { get; init; } = InvariantLayer.Fundamental;

        /// <summary>HIG-C criticality of the violated invariant.</summary>
        public InvariantCriticality Criticality { get; init; } = InvariantCriticality.Medium;

        /// <summary>HIG-C operational contexts in which the violated invariant is especially relevant.</summary>
        public System.Collections.Generic.IReadOnlyList<OperationalContext> Contexts { get; init; }
            = System.Array.Empty<OperationalContext>();

        /// <summary>Explanation returned by the invariant.</summary>
        public string Reason { get; init; } = "";
    }
}

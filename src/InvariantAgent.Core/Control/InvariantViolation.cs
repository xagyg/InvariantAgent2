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

        /// <summary>Severity of the violation; under the default policy Error and Critical can never be overridden.</summary>
        public InvariantSeverity Severity { get; init; }

        /// <summary>HIG level of the violated invariant.</summary>
        public InvariantLayer Layer { get; init; } = InvariantLayer.Fundamental;

        /// <summary>Base criticality of the violated invariant.</summary>
        public InvariantCriticality Criticality { get; init; } = InvariantCriticality.Medium;

        /// <summary>Criticality of this violation in the operational context it occurred in, C(I, κ).</summary>
        public InvariantCriticality CriticalityInContext { get; init; } = InvariantCriticality.Medium;

        /// <summary>Violation degree v ∈ (0, 1].</summary>
        public double Degree { get; init; } = 1;

        /// <summary>Operational contexts in which violating the invariant is more critical than usual.</summary>
        public System.Collections.Generic.IReadOnlyList<OperationalContext> Contexts { get; init; }
            = System.Array.Empty<OperationalContext>();

        /// <summary>Explanation returned by the invariant.</summary>
        public string Reason { get; init; } = "";
    }
}

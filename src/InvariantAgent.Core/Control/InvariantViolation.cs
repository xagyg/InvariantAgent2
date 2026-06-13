namespace InvariantAgent.Core.Model.Control
{
    public sealed class InvariantViolation
    {
        public string Invariant { get; init; } = "";

        public InvariantCategory Category { get; init; }

        public InvariantScope Scope { get; init; }

        public InvariantSeverity Severity { get; init; }

        public InvariantLayer Layer { get; init; } = InvariantLayer.Fundamental;

        public InvariantCriticality Criticality { get; init; } = InvariantCriticality.Medium;

        public System.Collections.Generic.IReadOnlyList<OperationalContext> Contexts { get; init; }
            = System.Array.Empty<OperationalContext>();

        public string Reason { get; init; } = "";
    }
}

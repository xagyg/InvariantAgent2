namespace InvariantAgent.Core.Model.Control
{
    public sealed class InvariantViolation
    {
        public string Invariant { get; init; } = "";

        public InvariantCategory Category { get; init; }

        public InvariantScope Scope { get; init; }

        public InvariantSeverity Severity { get; init; }

        public string Reason { get; init; } = "";
    }
}
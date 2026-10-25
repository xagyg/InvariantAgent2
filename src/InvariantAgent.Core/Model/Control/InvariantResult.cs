namespace InvariantAgent.Core.Model.Control
{
    /// <summary>The pass/fail result of evaluating a single invariant.</summary>
    public sealed class InvariantResult
    {
        /// <summary><c>true</c> if the invariant holds.</summary>
        public bool Passed { get; init; }

        /// <summary>Explanation for a failure.</summary>
        public string Reason { get; init; } = "";

        /// <summary>Severity reported for this result. Note: the evaluator uses the invariant's declared severity for violations.</summary>
        public InvariantSeverity Severity { get; init; } = InvariantSeverity.Error;

        /// <summary>Creates a passing result.</summary>
        public static InvariantResult Allow()
        {
            return new InvariantResult
            {
                Passed = true
            };
        }

        /// <summary>Creates a failing result.</summary>
        /// <param name="reason">Why the invariant failed.</param>
        /// <param name="severity">Severity of the failure (default Error).</param>
        public static InvariantResult Reject(string reason, InvariantSeverity severity = InvariantSeverity.Error)
        {
            return new InvariantResult
            {
                Passed = false,
                Reason = reason,
                Severity = severity
            };
        }
    }
}
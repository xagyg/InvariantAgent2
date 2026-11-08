using System;

namespace InvariantAgent.Core.Model.Control
{
    /// <summary>The result of evaluating a single invariant: pass/fail plus a violation degree.</summary>
    /// <remarks>
    /// HIG-C (IBAAM 3, revised edition, Section 3.2) grades violations: <see cref="Degree"/> is 0 when the invariant holds
    /// and in (0, 1] when it is violated. Boolean invariants use 0 or 1 (<see cref="Allow"/> / <see cref="Reject"/>);
    /// threshold invariants report the normalised shortfall with <see cref="Violated"/>.
    /// </remarks>
    public sealed class InvariantResult
    {
        private double? _degree;

        /// <summary><c>true</c> if the invariant holds.</summary>
        public bool Passed { get; init; }

        /// <summary>
        /// Violation degree v ∈ [0, 1]: 0 when the invariant holds, 1 for a full violation, and a value in between for a
        /// partial (threshold) violation. Defaults to 0 for a pass and 1 for a failure.
        /// </summary>
        public double Degree
        {
            get => Passed ? 0 : _degree ?? 1;
            init => _degree = Math.Clamp(value, 0, 1);
        }

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

        /// <summary>Creates a failing result with violation degree 1.</summary>
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

        /// <summary>
        /// Creates a graded result. A degree of 0 (or less) is a pass; anything greater is a violation of that degree,
        /// e.g. <c>max(0, (0.8 − coverage) / 0.8)</c> for "coverage is at least 80%".
        /// </summary>
        /// <param name="degree">Violation degree; clamped to [0, 1].</param>
        /// <param name="reason">Why the invariant is (partly) violated.</param>
        /// <param name="severity">Severity of the failure (default Error).</param>
        public static InvariantResult Violated(double degree, string reason, InvariantSeverity severity = InvariantSeverity.Error)
        {
            return degree <= 0
                ? Allow()
                : new InvariantResult
                {
                    Passed = false,
                    Degree = degree,
                    Reason = reason,
                    Severity = severity
                };
        }
    }
}

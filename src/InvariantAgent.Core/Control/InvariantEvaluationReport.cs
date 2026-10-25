using System.Collections.Generic;
using System;
using System.Linq;

namespace InvariantAgent.Core.Model.Control
{
    /// <summary>
    /// The outcome of evaluating all invariants for one lifecycle scope.
    /// </summary>
    public sealed class InvariantEvaluationReport
    {
        /// <summary>The lifecycle scope that was evaluated.</summary>
        public InvariantScope Scope { get; init; }

        /// <summary>
        /// <c>true</c> when no unresolved violations remain. Violations that were authorised as
        /// overrides do not prevent the report from passing.
        /// </summary>
        public bool Passed => Violations.Count == 0;

        /// <summary>Violations that remain blocking after override resolution.</summary>
        public IReadOnlyList<InvariantViolation> Violations { get; init; }
            = Array.Empty<InvariantViolation>();

        /// <summary>Lower-priority violations that were overridden under HIG meta-invariant policy.</summary>
        public IReadOnlyList<InvariantOverride> Overrides { get; init; }
            = Array.Empty<InvariantOverride>();

        /// <summary>Human-readable summary used as the rejection reason and in control events.</summary>
        public string Summary =>
            Passed
                ? Overrides.Count == 0
                    ? "All invariants passed."
                    : "All unresolved invariants passed; lower-priority violations were overridden."
                : string.Join(
                    Environment.NewLine,
                    Violations.Select(v =>
                        $"{v.Invariant}: {v.Reason}"));
    }
}

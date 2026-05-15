using System.Collections.Generic;
using System;
using System.Linq;

namespace InvariantAgent.Core.Model.Control
{
    public sealed class InvariantEvaluationReport
    {
        public InvariantScope Scope { get; init; }

        public bool Passed => Violations.Count == 0;

        public IReadOnlyList<InvariantViolation> Violations { get; init; }
            = Array.Empty<InvariantViolation>();

        public string Summary =>
            Passed
                ? "All invariants passed."
                : string.Join(
                    Environment.NewLine,
                    Violations.Select(v =>
                        $"{v.Invariant}: {v.Reason}"));
    }
}

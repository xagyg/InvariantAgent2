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

        public IReadOnlyList<InvariantOverride> Overrides { get; init; }
            = Array.Empty<InvariantOverride>();

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

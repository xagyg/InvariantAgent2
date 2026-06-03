using InvariantAgent.Core.Model.Transition;
using System;
using System.Collections.Generic;

namespace InvariantAgent.Core.Model.Control
{
    public sealed class MetaInvariantContext
    {
        public TransitionContext TransitionContext { get; init; } = default!;

        public InvariantViolation CandidateViolation { get; init; } = new();

        public IReadOnlyList<InvariantViolation> AllViolations { get; init; }
            = Array.Empty<InvariantViolation>();

        public IReadOnlyList<InvariantEvaluationRecord> EvaluationRecords { get; init; }
            = Array.Empty<InvariantEvaluationRecord>();
    }
}

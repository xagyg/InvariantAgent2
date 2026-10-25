using InvariantAgent.Core.Model.Transition;
using System;
using System.Collections.Generic;

namespace InvariantAgent.Core.Model.Control
{
    /// <summary>Input to a meta-invariant: one candidate violation plus everything evaluated alongside it.</summary>
    public sealed class MetaInvariantContext
    {
        /// <summary>The transition being governed.</summary>
        public TransitionContext TransitionContext { get; init; } = default!;

        /// <summary>The violation being considered for override.</summary>
        public InvariantViolation CandidateViolation { get; init; } = new();

        /// <summary>All violations raised in the same scope evaluation.</summary>
        public IReadOnlyList<InvariantViolation> AllViolations { get; init; }
            = Array.Empty<InvariantViolation>();

        /// <summary>All invariant evaluations (passed and failed) in the same scope evaluation.</summary>
        public IReadOnlyList<InvariantEvaluationRecord> EvaluationRecords { get; init; }
            = Array.Empty<InvariantEvaluationRecord>();
    }
}

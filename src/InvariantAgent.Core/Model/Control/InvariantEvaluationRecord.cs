using InvariantAgent.Core.Abstractions;

namespace InvariantAgent.Core.Model.Control
{
    /// <summary>Pairs an evaluated invariant with its result; consumed by meta-invariants.</summary>
    public sealed class InvariantEvaluationRecord
    {
        /// <summary>The invariant that was evaluated.</summary>
        public IInvariant Invariant { get; init; } = default!;

        /// <summary>The evaluation result.</summary>
        public InvariantResult Result { get; init; } = default!;
    }
}

using InvariantAgent.Core.Abstractions;

namespace InvariantAgent.Core.Model.Control
{
    public sealed class InvariantEvaluationRecord
    {
        public IInvariant Invariant { get; init; } = default!;

        public InvariantResult Result { get; init; } = default!;
    }
}

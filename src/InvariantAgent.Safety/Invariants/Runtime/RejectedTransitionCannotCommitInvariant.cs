using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Control;
using InvariantAgent.Core.Model.Transition;

namespace InvariantAgent.Safety.Invariants.Runtime
{
    /// <summary>
    /// Reduction-scope lifecycle invariant: a rejected transition must not carry a proposed next state.
    /// Registered by default as a backstop alongside the runtime's own rejection handling.
    /// </summary>
    public sealed class RejectedTransitionCannotCommitInvariant : IInvariant
    {
        public string Name => nameof(RejectedTransitionCannotCommitInvariant);

        public InvariantCategory Category => InvariantCategory.Integrity;

        public InvariantScope Scope => InvariantScope.Reduction;

        public InvariantSeverity Severity => InvariantSeverity.Critical;

        /// <inheritdoc />
        public InvariantResult Evaluate(TransitionContext context)
        {
            var transition = context.Transition;

            if (transition.Status != TransitionStatus.Rejected)
            {
                return InvariantResult.Allow();
            }

            if (transition.After != null)
            {
                return InvariantResult.Reject("Rejected transitions cannot produce committed state.", Severity);
            }

            return InvariantResult.Allow();
        }
    }
}
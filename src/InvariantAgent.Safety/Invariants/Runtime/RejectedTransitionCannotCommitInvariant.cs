using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Control;
using InvariantAgent.Core.Model.Transition;

namespace InvariantAgent.Safety.Invariants.Runtime
{
    public sealed class RejectedTransitionCannotCommitInvariant : IInvariant
    {
        public string Name => nameof(RejectedTransitionCannotCommitInvariant);

        public InvariantCategory Category => InvariantCategory.Integrity;

        public InvariantScope Scope => InvariantScope.Reduction;

        public InvariantSeverity Severity => InvariantSeverity.Critical;

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
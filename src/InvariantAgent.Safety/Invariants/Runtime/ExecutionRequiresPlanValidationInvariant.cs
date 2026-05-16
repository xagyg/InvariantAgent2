using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Control;
using InvariantAgent.Core.Model.Transition;
using InvariantAgent.Core.Transitioning;

namespace InvariantAgent.Safety.Invariants.Runtime
{
    public sealed class ExecutionRequiresPlanValidationInvariant
        : IInvariant
    {
        public string Name => nameof(ExecutionRequiresPlanValidationInvariant);

        public InvariantCategory Category => InvariantCategory.Integrity;

        public InvariantScope Scope => InvariantScope.Execution;

        public InvariantSeverity Severity => InvariantSeverity.Critical;

        public InvariantResult Evaluate(TransitionContext context)
        {
            var transition = context.Transition;

            if (!TransitionLifecycle.HasReached(transition, TransitionPhase.PlanValidation))
            {
                return InvariantResult.Reject("Execution requires successful plan validation.", Severity);
            }

            return InvariantResult.Allow();
        }
    }
}
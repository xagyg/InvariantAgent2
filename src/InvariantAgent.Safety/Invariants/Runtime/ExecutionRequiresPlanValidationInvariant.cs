using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Control;
using InvariantAgent.Core.Model.Transition;
using InvariantAgent.Core.Transitioning;

namespace InvariantAgent.Safety.Invariants.Runtime
{
    /// <summary>
    /// Execution-scope lifecycle invariant: execution must not occur unless the transition passed through
    /// plan validation. Not registered by default.
    /// </summary>
    public sealed class ExecutionRequiresPlanValidationInvariant
        : IInvariant
    {
        public string Name => nameof(ExecutionRequiresPlanValidationInvariant);

        public InvariantCategory Category => InvariantCategory.Integrity;

        public InvariantScope Scope => InvariantScope.Execution;

        public InvariantSeverity Severity => InvariantSeverity.Critical;

        /// <inheritdoc />
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
using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Control;
using InvariantAgent.Core.Model.Transition;

namespace InvariantAgent.Safety.Invariants.Action
{
    public sealed class NoEmptyInputInvariant : IInvariant
    {
        public string Name => nameof(NoEmptyInputInvariant);

        public InvariantCategory Category => InvariantCategory.Integrity;

        public InvariantScope Scope => InvariantScope.Plan;

        public InvariantSeverity Severity => InvariantSeverity.Error;

        public InvariantResult Evaluate(TransitionContext context)
        {
            var action = context.Transition.ProposedAction;

            if (action == null)
            {
                return InvariantResult.Reject("No proposed action.");
            }

            if (string.IsNullOrWhiteSpace(action.Input))
            {
                return InvariantResult.Reject("Empty input not allowed.");
            }

            return InvariantResult.Allow();
        }
    }
}
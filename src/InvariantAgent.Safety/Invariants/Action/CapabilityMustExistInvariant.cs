using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Control;
using InvariantAgent.Core.Model.Transition;

namespace InvariantAgent.Safety.Invariants.Action
{
    public sealed class CapabilityMustExistInvariant : IInvariant
    {
        public string Name => nameof(CapabilityMustExistInvariant);

        public InvariantCategory Category => InvariantCategory.Integrity;

        public InvariantResult Evaluate(TransitionContext context)
        {
            var action = context.Transition.ProposedAction;

            if (action == null)
            {
                return InvariantResult.Reject("No proposed action.");
            }

            if (string.IsNullOrWhiteSpace(action.Capability))
            {
                return InvariantResult.Reject("No tool selected.");
            }

            return InvariantResult.Allow();
        }
    }
}
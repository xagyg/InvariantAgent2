using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Control;
using InvariantAgent.Core.Model.Transition;

namespace InvariantAgent.Safety.Invariants.Outcome
{
    public sealed class NonEmptyOutcomeInvariant : IInvariant
    {
        public string Name => nameof(NonEmptyOutcomeInvariant);

        public InvariantCategory Category => InvariantCategory.Integrity;

        public InvariantResult Evaluate(TransitionContext context)
        {
            var outcome = context.Transition.Outcome;

            if (outcome == null)
            {
                return InvariantResult.Reject("No execution outcome.");
            }

            if (string.IsNullOrWhiteSpace(outcome.Result))
            {
                return InvariantResult.Reject("Outcome is empty.");
            }

            return InvariantResult.Allow();
        }
    }
}
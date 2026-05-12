using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Control;
using InvariantAgent.Core.Model.Transition;

namespace InvariantAgent.Safety.Invariants.Outcome
{
    public sealed class SuccessOutcomeInvariant : IInvariant
    {
        public string Name => nameof(SuccessOutcomeInvariant);

        public InvariantCategory Category => InvariantCategory.Integrity;

        public InvariantResult Evaluate(TransitionContext context)
        {
            var outcome = context.Transition.Outcome;

            if (outcome == null)
            {
                return InvariantResult.Reject("No execution outcome.");
            }

            if (!outcome.Success)
            {
                return InvariantResult.Reject(
                    string.IsNullOrWhiteSpace(outcome.Error)
                        ? "Execution failed."
                        : outcome.Error);
            }

            return InvariantResult.Allow();
        }
    }
}
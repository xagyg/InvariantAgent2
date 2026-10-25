using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Control;
using InvariantAgent.Core.Model.Transition;

namespace InvariantAgent.Safety.Invariants.Action
{
    /// <summary>
    /// Plan-scope integrity invariant: the proposed action must have non-empty input. Not registered by default.
    /// </summary>
    public sealed class NonEmptyInputInvariant : IInvariant
    {
        public string Name => nameof(NonEmptyInputInvariant);

        public InvariantCategory Category => InvariantCategory.Integrity;

        public InvariantScope Scope => InvariantScope.Plan;

        public InvariantSeverity Severity => InvariantSeverity.Error;

        /// <inheritdoc />
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
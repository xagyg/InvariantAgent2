using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Control;
using InvariantAgent.Core.Model.Transition;

namespace InvariantAgent.Safety.Invariants.Action
{
    /// <summary>
    /// Plan-scope safety invariant: forbids the <c>delete</c> capability. An example hard safety constraint.
    /// </summary>
    public sealed class NoDeleteInvariant : IInvariant
    {
        public string Name => nameof(NoDeleteInvariant);

        public InvariantCategory Category => InvariantCategory.Safety;

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

            if (action.Capability == "delete")
            {
                return InvariantResult.Reject("Delete operations are not allowed.");
            }

            return InvariantResult.Allow();
        }
    }
}
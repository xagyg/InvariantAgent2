using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Control;
using InvariantAgent.Core.Model.Transition;

namespace InvariantAgent.Safety.Invariants.Action
{
    /// <summary>
    /// Plan-scope integrity invariant: a capability must have been selected. Not registered by default
    /// (<see cref="AllowedCapabilityInvariant"/> covers this case).
    /// </summary>
    public sealed class CapabilityMustExistInvariant : IInvariant
    {
        public string Name => nameof(CapabilityMustExistInvariant);

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

            if (string.IsNullOrWhiteSpace(action.Capability))
            {
                return InvariantResult.Reject("No tool selected.");
            }

            return InvariantResult.Allow();
        }
    }
}
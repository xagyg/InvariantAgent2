using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Control;
using InvariantAgent.Core.Model.Transition;

namespace InvariantAgent.Safety.Invariants.Action
{
    public sealed class AllowedCapabilityInvariant : IInvariant
    {
        private readonly HashSet<string> _allowedCapabilities;

        public string Name => nameof(AllowedCapabilityInvariant);

        public InvariantCategory Category => InvariantCategory.Safety;

        public InvariantScope Scope => InvariantScope.Plan;

        public InvariantSeverity Severity => InvariantSeverity.Error;

        public AllowedCapabilityInvariant(IEnumerable<string> allowedCapabilities)
        {
            _allowedCapabilities = new HashSet<string>(
                allowedCapabilities);
        }

        public InvariantResult Evaluate(TransitionContext context)
        {
            var action = context.Transition.ProposedAction;

            if (action == null)
            {
                return InvariantResult.Reject("No proposed action.", Severity);
            }

            if (string.IsNullOrWhiteSpace(action.Capability))
            {
                return InvariantResult.Reject("No capability selected.", Severity);
            }

            if (_allowedCapabilities.Contains(action.Capability))
            {
                return InvariantResult.Allow();
            }

            return InvariantResult.Reject($"Capability '{action.Capability}' is not allowed or unknown.", Severity);
        }
    }
}
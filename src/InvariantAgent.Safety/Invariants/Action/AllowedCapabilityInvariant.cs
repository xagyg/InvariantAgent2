using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Control;
using InvariantAgent.Core.Model.Transition;

namespace InvariantAgent.Safety.Invariants.Action
{
    /// <summary>
    /// Plan-scope safety invariant: the proposed capability must be on the allow-list.
    /// </summary>
    /// <remarks>
    /// The allow-list is seeded from the capability registry, so this also rejects unknown capabilities
    /// and planner error actions before any execution takes place.
    /// </remarks>
    public sealed class AllowedCapabilityInvariant : IInvariant
    {
        private readonly HashSet<string> _allowedCapabilities;

        public string Name => nameof(AllowedCapabilityInvariant);

        public InvariantCategory Category => InvariantCategory.Safety;

        public InvariantScope Scope => InvariantScope.Plan;

        public InvariantSeverity Severity => InvariantSeverity.Error;

        /// <summary>Creates the invariant.</summary>
        /// <param name="allowedCapabilities">Capability names and aliases that may be invoked.</param>
        public AllowedCapabilityInvariant(IEnumerable<string> allowedCapabilities)
        {
            _allowedCapabilities = new HashSet<string>(
                allowedCapabilities);
        }

        /// <inheritdoc />
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

            // Note: case-sensitive match, whereas the registry itself resolves names case-insensitively.
            if (_allowedCapabilities.Contains(action.Capability))
            {
                return InvariantResult.Allow();
            }

            return InvariantResult.Reject($"Capability '{action.Capability}' is not allowed or unknown.", Severity);
        }
    }
}
using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Control;
using InvariantAgent.Core.Model.Transition;

namespace InvariantAgent.Safety.Invariants.SelfModification
{
    public sealed class AllowedMemoryKeyInvariant : IInvariant
    {
        private readonly HashSet<string> _allowedKeys =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "goal",
                "user_intent",
                "last_action",
                "notes"
            };

        public string Name => nameof(AllowedMemoryKeyInvariant);

        public InvariantCategory Category =>
            InvariantCategory.SelfModification;

        public InvariantResult Evaluate(TransitionContext context)
        {
            var modification =
                context.Transition.SelfModification;

            if (modification == null)
                return InvariantResult.Allow();

            if (modification.Target != "memory")
                return InvariantResult.Allow();

            if (_allowedKeys.Contains(modification.Key))
                return InvariantResult.Allow();

            return InvariantResult.Reject(
                $"Memory key '{modification.Key}' is not allowed.");
        }
    }
}
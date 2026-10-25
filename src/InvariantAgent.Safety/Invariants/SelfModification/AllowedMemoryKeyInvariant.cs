using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Control;
using InvariantAgent.Core.Model.Transition;

namespace InvariantAgent.Safety.Invariants.SelfModification
{
    /// <summary>
    /// Self-modification invariant: memory updates may only target allow-listed keys.
    /// </summary>
    /// <remarks>Bounds the agent's ability to rewrite its own memory (governed self-modification).</remarks>
    public sealed class AllowedMemoryKeyInvariant : IInvariant
    {
        // Memory keys that capabilities may propose to change (case-insensitive).
        private readonly HashSet<string> _allowedKeys =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "goal",
                "user_intent",
                "last_action",
                "notes"
            };

        public string Name => nameof(AllowedMemoryKeyInvariant);

        public InvariantScope Scope => InvariantScope.SelfModification;

        public InvariantSeverity Severity => InvariantSeverity.Error;

        public InvariantCategory Category => InvariantCategory.SelfModification;

        /// <inheritdoc />
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
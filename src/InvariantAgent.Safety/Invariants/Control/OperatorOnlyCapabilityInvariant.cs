using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Control;
using InvariantAgent.Core.Model.Transition;
using InvariantAgent.Core.Parsing;

namespace InvariantAgent.Safety.Invariants.Control
{
    /// <summary>
    /// Plan-scope control-integrity invariant: an operator-only capability (<see cref="ICapability.OperatorOnly"/>) may run
    /// only when the operator typed exactly that command.
    /// </summary>
    /// <remarks>
    /// <para>
    /// IBAAM 1 (Section 4.3) requires that the control layer "must not be freely modifiable by the agent itself". Resolving
    /// conscience escalations, re-certifying the drift baseline and changing the HIG-C operational context are control
    /// actions. If a planner could propose them, an LLM planner could overturn its own escalations or move the baseline its
    /// drift is scored against, and the reviewer signal IBAAM 6 relies on (Section 5.2) would no longer be independent.
    /// </para>
    /// <para>
    /// The check is structural, not a prompt instruction: the proposed action must equal the literal parse of the raw input.
    /// With the command planner that is always true, so the REPL works as before. With an LLM planner the operator can still
    /// type the command verbatim, but a model that proposes one on its own (from a paraphrase, an injected instruction or a
    /// revision) is blocked. The decision depends only on the recorded input and action, so replay reproduces it.
    /// </para>
    /// </remarks>
    public sealed class OperatorOnlyCapabilityInvariant : IInvariant
    {
        private readonly HashSet<string> _operatorOnly;

        /// <summary>Creates the invariant.</summary>
        /// <param name="operatorOnlyCapabilities">Names (and aliases) of operator-only capabilities.</param>
        public OperatorOnlyCapabilityInvariant(IEnumerable<string> operatorOnlyCapabilities)
        {
            _operatorOnly = new HashSet<string>(operatorOnlyCapabilities, StringComparer.OrdinalIgnoreCase);
        }

        /// <inheritdoc />
        public string Name => nameof(OperatorOnlyCapabilityInvariant);

        /// <inheritdoc />
        public InvariantCategory Category => InvariantCategory.Safety;

        /// <inheritdoc />
        public InvariantScope Scope => InvariantScope.Plan;

        /// <inheritdoc />
        public InvariantSeverity Severity => InvariantSeverity.Critical;

        /// <inheritdoc />
        public string ProtectedValue => ProtectedValues.ControlIntegrity;

        /// <summary>The operator-only capability names this invariant guards.</summary>
        public IReadOnlyCollection<string> OperatorOnlyCapabilities => _operatorOnly;

        /// <inheritdoc />
        public InvariantResult Evaluate(TransitionContext context)
        {
            var transition = context.Transition;
            var action = transition.ProposedAction;

            if (action == null || string.IsNullOrWhiteSpace(action.Capability) ||
                !_operatorOnly.Contains(action.Capability.Trim()))
            {
                return InvariantResult.Allow();
            }

            if (IsOperatorAuthored(transition.Input, action.Capability, action.Input))
            {
                return InvariantResult.Allow();
            }

            return InvariantResult.Reject(
                $"Capability '{action.Capability}' is operator-only and the planner proposed it; " +
                "the operator must type the command directly.",
                Severity);
        }

        // The operator authored the action when it is exactly what they typed (capability case-insensitive, input verbatim
        // apart from surrounding whitespace).
        private static bool IsOperatorAuthored(string rawInput, string capability, string input)
        {
            if (string.IsNullOrWhiteSpace(rawInput))
            {
                return false;
            }

            var typed = AgentActionParser.Parse(rawInput);

            return string.Equals(typed.Capability, capability.Trim(), StringComparison.OrdinalIgnoreCase) &&
                   string.Equals((typed.Input ?? "").Trim(), (input ?? "").Trim(), StringComparison.Ordinal);
        }
    }
}

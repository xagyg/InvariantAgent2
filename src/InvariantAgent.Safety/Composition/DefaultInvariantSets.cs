using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model;
using InvariantAgent.Safety.Invariants.Action;
using InvariantAgent.Safety.Invariants.Outcome;

namespace InvariantAgent.Safety.Composition
{
    public static class DefaultInvariantSets
    {
        public static InvariantSet<AgentAction> BuildActionSet()
        {
            return new InvariantSet<AgentAction>(new IActionInvariant[]
            {
            new NoDeleteInvariant(),
            new AllowedCapabilityInvariant(new[] { "search", "echo", "calculator" })
            });
        }

        public static InvariantSet<AgentOutcome> BuildOutcomeSet()
        {
            return new InvariantSet<AgentOutcome>(new IOutcomeInvariant[]
            {
            new SuccessOutcomeInvariant(),
            new NonEmptyOutcomeInvariant()
            });
        }
    }
}

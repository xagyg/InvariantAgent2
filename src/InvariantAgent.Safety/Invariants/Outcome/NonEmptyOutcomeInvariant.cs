using InvariantAgent.Core.Model;
using InvariantAgent.Core.Abstractions;
using InvariantAgent.Safety.Invariants.Action;

namespace InvariantAgent.Safety.Invariants.Outcome
{
    public class NonEmptyOutcomeInvariant : IOutcomeInvariant
    {
        public string Name => nameof(NonEmptyOutcomeInvariant);
        public InvariantResult Evaluate(AgentOutcome outcome)
        {
            if (outcome.Result == null)
                return InvariantResult.Fail(Name, "Outcome is empty");

            return InvariantResult.Pass(Name);
        }
    }
}

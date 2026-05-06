using InvariantAgent.Core.Model;
using InvariantAgent.Core.Abstractions;

namespace InvariantAgent.Safety.Invariants.Outcome
{
    public class NonEmptyOutcomeInvariant : IOutcomeInvariant
    {
        public InvariantResult Evaluate(AgentOutcome outcome)
        {
            if (outcome.Result == null)
                return InvariantResult.Fail("Outcome is empty");

            return InvariantResult.Pass();
        }
    }
}

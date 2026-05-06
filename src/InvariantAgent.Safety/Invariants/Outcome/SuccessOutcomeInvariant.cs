using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model;

namespace InvariantAgent.Safety.Invariants.Outcome
{
    public class SuccessOutcomeInvariant : IOutcomeInvariant
    {
        public InvariantResult Evaluate(AgentOutcome outcome)
        {
            if (outcome.Result == null)
                return InvariantResult.Fail("Execution failed");

            return InvariantResult.Pass();
        }
    }
}

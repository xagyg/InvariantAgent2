using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model;

namespace InvariantAgent.Safety.Invariants.Outcome
{
    public class SuccessOutcomeInvariant : IOutcomeInvariant
    {
        public string Name => nameof(SuccessOutcomeInvariant);

        public InvariantResult Evaluate(AgentOutcome outcome)
        {
            if (outcome.Result == null)
                return InvariantResult.Fail(Name, "Execution failed");

            if (!outcome.Result.Success)
                return InvariantResult.Fail(Name, outcome.Result.Error);

            return InvariantResult.Pass(Name);
        }
    }
}

using InvariantAgent.Core.Model;
using InvariantAgent.Core.Abstractions;

namespace InvariantAgent.Safety.Invariants.Action
{
    public class NoEmptyInputInvariant : IActionInvariant
    {
        public InvariantResult Evaluate(AgentAction input)
        {
            if (string.IsNullOrWhiteSpace(input.Input))
                return InvariantResult.Fail("Empty input not allowed");

            return InvariantResult.Pass();
        }
    }
}

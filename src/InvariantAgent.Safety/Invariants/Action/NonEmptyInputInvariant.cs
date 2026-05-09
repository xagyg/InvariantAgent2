using InvariantAgent.Core.Model;
using InvariantAgent.Core.Abstractions;

namespace InvariantAgent.Safety.Invariants.Action
{
    public class NoEmptyInputInvariant : IActionInvariant
    {
        public string Name => nameof(NoEmptyInputInvariant);

        public InvariantResult Evaluate(AgentAction input)
        {
            if (string.IsNullOrWhiteSpace(input.Input))
                return InvariantResult.Fail(Name, "Empty input not allowed");

            return InvariantResult.Pass(Name);
        }
    }
}

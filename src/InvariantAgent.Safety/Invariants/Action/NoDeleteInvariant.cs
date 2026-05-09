using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model;

namespace InvariantAgent.Safety.Invariants.Action
{
    public class NoDeleteInvariant : IActionInvariant
    {
        public string Name => nameof(NoDeleteInvariant);

        public InvariantResult Evaluate(AgentAction action)
        {
            if (action.Tool == "delete")
                return InvariantResult.Fail(Name, "Delete operations are not allowed");

            return InvariantResult.Pass(Name);
        }
    }
}

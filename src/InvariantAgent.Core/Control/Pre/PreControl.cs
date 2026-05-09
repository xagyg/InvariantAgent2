using InvariantAgent.Core.Model;
using InvariantAgent.Core.Abstractions;

namespace InvariantAgent.Core.Control.Pre
{

    public class PreControl : IPreControl
    {
        private readonly InvariantSet<AgentAction> _invariants;

        public PreControl(InvariantSet<AgentAction> invariants)
        {
            _invariants = invariants;
        }

        public PreControlResult Evaluate(AgentState state, AgentAction action)
        {
            var result = _invariants.Evaluate(action);

            return new PreControlResult
            {
                Allowed = result.IsValid,
                Reason = $"{result.InvariantName}: {result.Reason}"
            };
        }
    }
}

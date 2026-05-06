using InvariantAgent.Core.Model;

namespace InvariantAgent.Core.Control.Pre
{

    public class PreControl
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
                Reason = result.Reason
            };
        }
    }
}

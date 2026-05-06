using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model;

namespace InvariantAgent.Core.Control
{
    public class ControlOperator : IControlOperator
    {
        private readonly IPreControl _pre;
        private readonly IPostControl _post;

        public ControlOperator(IPreControl pre, IPostControl post)
        {
            _pre = pre;
            _post = post;
        }

        public AgentAction ApplyPre(AgentState state, AgentAction action)
        {
            var result = _pre.Evaluate(state, action);
            return result.Allowed ? action : null;
        }

        public AgentOutcome ApplyPost(AgentState state, AgentOutcome outcome)
        {
            var result = _post.Evaluate(state, outcome);
            return result.Accepted ? outcome : null;
        }
    }
}

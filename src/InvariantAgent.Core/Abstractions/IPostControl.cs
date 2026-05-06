using InvariantAgent.Core.Control.Post;
using InvariantAgent.Core.Model;

namespace InvariantAgent.Core.Abstractions;

public interface IPostControl
{
    PostControlResult Evaluate(AgentState state, AgentOutcome outcome);
}
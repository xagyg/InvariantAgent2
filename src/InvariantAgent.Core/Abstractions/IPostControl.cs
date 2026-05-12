using InvariantAgent.Core.Control.Post;
using InvariantAgent.Core.Model.Transition;

namespace InvariantAgent.Core.Abstractions
{
    public interface IPostControl
    {
        PostControlResult Evaluate(TransitionContext context);
    }
}
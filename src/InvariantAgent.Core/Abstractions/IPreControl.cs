using InvariantAgent.Core.Model.Control;
using InvariantAgent.Core.Model.Transition;

namespace InvariantAgent.Core.Abstractions
{
    public interface IPreControl
    {
        ControlDecision Evaluate(TransitionContext context);
    }
}
using InvariantAgent.Core.Model.Control;
using InvariantAgent.Core.Model.Transition;

namespace InvariantAgent.Core.Abstractions
{
    public interface IInvariantEvaluator
    {
        InvariantEvaluationReport Evaluate(TransitionContext context, InvariantScope scope);
    }
}
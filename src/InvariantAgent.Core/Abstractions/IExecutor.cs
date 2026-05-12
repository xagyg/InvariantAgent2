using InvariantAgent.Core.Model;
using InvariantAgent.Core.Model.Transition;

namespace InvariantAgent.Core.Abstractions;

public interface IExecutor
{
    void Execute(TransitionContext context);
}
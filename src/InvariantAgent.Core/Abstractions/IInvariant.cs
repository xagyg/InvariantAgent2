using InvariantAgent.Core.Model.Control;
using InvariantAgent.Core.Model.Transition;

namespace InvariantAgent.Core.Abstractions
{
    public interface IInvariant
    {
        string Name { get; }

        InvariantCategory Category { get; }

        InvariantResult Evaluate(TransitionContext context);
    }
}
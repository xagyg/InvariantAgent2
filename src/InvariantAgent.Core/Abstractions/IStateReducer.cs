using InvariantAgent.Core.Model.Transition;

namespace InvariantAgent.Core.Abstractions
{
    public interface IStateReducer
    {
        void Apply(TransitionContext context);
    }
}
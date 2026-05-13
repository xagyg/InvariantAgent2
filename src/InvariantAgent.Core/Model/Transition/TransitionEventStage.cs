
namespace InvariantAgent.Core.Model.Transition
{
    public enum TransitionEventStage
    {
        Input,
        Planning,
        PreInvariant,
        PreControl,
        Execution,
        PostInvariant,
        PostControl,
        Reduction,
        SelfModification
    }
}

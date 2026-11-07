
namespace InvariantAgent.Core.Model.Transition
{
    /// <summary>The pipeline stage that raised a <see cref="TransitionEvent"/>.</summary>
    public enum TransitionEventStage
    {
        Input,
        Planning,
        Invariant,
        Control,
        Execution, 
        Reduction,
        SelfModification,
        Lifecycle,
        /// <summary>Conscience appraisal (IBAAM 6). Appended last because stages are persisted as numbers.</summary>
        Conscience
    }
}

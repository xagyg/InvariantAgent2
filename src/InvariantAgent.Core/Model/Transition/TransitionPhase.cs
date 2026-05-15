namespace InvariantAgent.Core.Model.Transition
{
    public enum TransitionPhase
    {
        Created,
        InputReceived,
        Planning,
        PlanValidation,
        Execution,
        ExecutionValidation,
        SelfModificationValidation,
        Reduction,
        Completed,
        Rejected
    }
}
namespace InvariantAgent.Core.Model.Transition
{
    /// <summary>
    /// Ordered lifecycle phases of a transition. Ordering matters: <c>TransitionLifecycle</c> compares
    /// phases numerically, and legal moves are defined in <c>TransitionPhases</c>.
    /// </summary>
    public enum TransitionPhase
    {
        /// <summary>Transition object created.</summary>
        Created,
        /// <summary>User input recorded.</summary>
        InputReceived,
        /// <summary>Planner is proposing an action.</summary>
        Planning,
        /// <summary>Pre-control: plan invariants are evaluated.</summary>
        PlanValidation,
        /// <summary>Capability is executed.</summary>
        Execution,
        /// <summary>Post-control: execution invariants are evaluated.</summary>
        ExecutionValidation,
        /// <summary>Proposed self-modifications are evaluated.</summary>
        SelfModificationValidation,
        /// <summary>Reducer builds the next state and reduction invariants are evaluated.</summary>
        Reduction,
        /// <summary>Terminal: state committed.</summary>
        Completed,
        /// <summary>Terminal: rejected by governance; no state committed.</summary>
        Rejected
    }
}
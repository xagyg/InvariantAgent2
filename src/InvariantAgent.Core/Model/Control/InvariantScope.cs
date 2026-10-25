
namespace InvariantAgent.Core.Model.Control
{
    /// <summary>The lifecycle checkpoint at which an invariant is evaluated.</summary>
    public enum InvariantScope
    {
        /// <summary>Evaluated at every checkpoint.</summary>
        Transition,
        /// <summary>Pre-control: validates the planner's proposed action before execution.</summary>
        Plan,
        /// <summary>Post-control: validates the execution outcome.</summary>
        Execution,
        /// <summary>Validates agent state (reserved).</summary>
        State,
        /// <summary>Validates memory contents (reserved).</summary>
        Memory,
        /// <summary>Validates the reducer's proposed next state before it is committed.</summary>
        Reduction,
        /// <summary>Validates proposed self-modifications before they are assimilated.</summary>
        SelfModification,
        /// <summary>Validates observability concerns (reserved).</summary>
        Observability
    }
}

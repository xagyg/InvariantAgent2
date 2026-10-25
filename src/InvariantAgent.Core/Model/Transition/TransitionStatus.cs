namespace InvariantAgent.Core.Model.Transition
{
    /// <summary>Classification of a transition's progress or result.</summary>
    public enum TransitionStatus
    {
        /// <summary>The planner has proposed an action.</summary>
        Proposed,
        /// <summary>Allowed by governance (reserved).</summary>
        Allowed,
        /// <summary>Rejected by governance.</summary>
        Rejected,
        /// <summary>The capability has executed.</summary>
        Executed,
        /// <summary>The transition completed (or the reducer produced the next state).</summary>
        Completed,
        /// <summary>Execution could not proceed (e.g. no proposed action).</summary>
        Failed
    }
}
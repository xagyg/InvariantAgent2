namespace InvariantAgent.Core.Model.Conscience
{
    /// <summary>
    /// The four stances a functional conscience can take on a plan (IBAAM 6, Section 6.3).
    /// </summary>
    /// <remarks>
    /// Every stance is contained by the controls: none of them can authorise an action an invariant blocks.
    /// Numeric values are persisted, so new members must only ever be appended.
    /// </remarks>
    public enum ConscienceStance
    {
        /// <summary>No change to the pipeline.</summary>
        Proceed,

        /// <summary>
        /// Return the appraisal to the planner for a bounded number of re-plans. Every revised plan passes every
        /// plan invariant again and is re-appraised. At the answer appraisal point Revise becomes a caveat.
        /// </summary>
        Revise,

        /// <summary>Route the decision to human review; nothing is executed or committed meanwhile.</summary>
        Escalate,

        /// <summary>Reject through the same path as a failed control, with the justification logged.</summary>
        Veto
    }
}

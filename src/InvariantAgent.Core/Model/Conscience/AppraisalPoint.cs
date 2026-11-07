namespace InvariantAgent.Core.Model.Conscience
{
    /// <summary>
    /// Where in the transition lifecycle the conscience appraises (IBAAM 6, Section 5 and Figure 1).
    /// </summary>
    /// <remarks>Numeric values are persisted, so new members must only ever be appended.</remarks>
    public enum AppraisalPoint
    {
        /// <summary>
        /// Plan appraisal: after plan invariants pass and before execution, so the conscience sees the action that
        /// will actually run.
        /// </summary>
        Plan,

        /// <summary>
        /// Answer appraisal: after execution, self-modification and reduction invariants pass and before the new
        /// state is committed, so the conscience can weigh the actual outcome and resulting state.
        /// </summary>
        Answer
    }
}

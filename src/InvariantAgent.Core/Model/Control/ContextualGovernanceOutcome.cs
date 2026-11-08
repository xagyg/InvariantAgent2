namespace InvariantAgent.Core.Model.Control
{
    /// <summary>How criticality and context entered an override decision under HIG-C.</summary>
    public enum ContextualGovernanceOutcome
    {
        /// <summary>Priority between levels decided the override; criticality and context did not.</summary>
        NotApplicable,
        /// <summary>Retained for compatibility with records written before the revised HIG-C rule; no longer produced.</summary>
        Preserve,
        /// <summary>
        /// A same-level comparison decided the override: every competing candidate is worse at the invariant's own level,
        /// by criticality in the current context.
        /// </summary>
        Subordinate,
        /// <summary>Retained for compatibility with records written before the revised HIG-C rule; no longer produced.</summary>
        Unresolved
    }
}

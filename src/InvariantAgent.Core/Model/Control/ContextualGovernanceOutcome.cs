namespace InvariantAgent.Core.Model.Control
{
    /// <summary>Outcome of HIG-C same-layer conflict resolution.</summary>
    public enum ContextualGovernanceOutcome
    {
        /// <summary>No same-layer conflict existed; HIG-C did not apply.</summary>
        NotApplicable,
        /// <summary>The invariant scored highest and must be preserved (it stays blocking and is not overridden).</summary>
        Preserve,
        /// <summary>A conflicting invariant scored higher, so this one may be subordinated (overridden) if meta-invariants allow.</summary>
        Subordinate,
        /// <summary>Scores were tied; HIG-C could not distinguish the conflicting invariants.</summary>
        Unresolved
    }
}

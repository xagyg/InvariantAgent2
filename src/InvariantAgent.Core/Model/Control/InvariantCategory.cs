namespace InvariantAgent.Core.Model.Control
{
    /// <summary>The behavioural concern an invariant protects. Used for grouping in drift reports.</summary>
    public enum InvariantCategory
    {
        /// <summary>Prevents harmful or disallowed actions.</summary>
        Safety,
        /// <summary>Protects correctness of execution, outcomes and lifecycle.</summary>
        Integrity,
        /// <summary>Bounds behavioural drift so the agent's behaviour remains recognisably stable.</summary>
        Identity,
        /// <summary>Governs changes the agent proposes to its own state.</summary>
        SelfModification
    }
}
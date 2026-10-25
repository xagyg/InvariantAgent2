namespace InvariantAgent.Core.Model.Control
{
    /// <summary>The governance concern addressed by a meta-invariant.</summary>
    public enum MetaInvariantCategory
    {
        /// <summary>Hierarchy precedence between layers.</summary>
        Priority,
        /// <summary>Which severities may be overridden.</summary>
        OverrideSeverity,
        /// <summary>Requirement for an inspectable override rationale.</summary>
        Justification,
        /// <summary>Marking overrides for governance audit.</summary>
        Audit,
        /// <summary>Flagging ambiguous overrides for human review.</summary>
        Review
    }
}

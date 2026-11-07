namespace InvariantAgent.Core.Model.Control
{
    /// <summary>
    /// Protected-value tags shared by invariants and conscience commitments (IBAAM 6, Section 6.4).
    /// </summary>
    public static class ProtectedValues
    {
        /// <summary>Stability of the agent's behavioural identity (drift).</summary>
        public const string IdentityStability = "identity-stability";

        /// <summary>Staying inside the controls: used when the concern is anticipated rejection with no more specific tag.</summary>
        public const string ControlBoundary = "control-boundary";

        /// <summary>Consistency with the agent's own prior commitments.</summary>
        public const string Consistency = "consistency";

        /// <summary>The control layer stays outside the agent's reach (IBAAM 1, Section 4.3).</summary>
        public const string ControlIntegrity = "control-integrity";

        /// <summary>Default tag for an invariant category.</summary>
        /// <param name="category">The invariant category.</param>
        public static string ForCategory(InvariantCategory category)
        {
            switch (category)
            {
                case InvariantCategory.Safety:
                    return "safety";
                case InvariantCategory.Integrity:
                    return "integrity";
                case InvariantCategory.Identity:
                    return IdentityStability;
                case InvariantCategory.SelfModification:
                    return "self-modification";
                default:
                    return category.ToString().ToLowerInvariant();
            }
        }
    }
}

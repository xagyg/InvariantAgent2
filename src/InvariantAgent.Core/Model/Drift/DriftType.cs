namespace InvariantAgent.Core.Model.Drift
{
    /// <summary>Kinds of drift tracked by the observability layer.</summary>
    public enum DriftType
    {
        /// <summary>No drift.</summary>
        None,

        /// <summary>Re-evaluating invariants on replay produced a different decision than originally recorded.</summary>
        GovernanceDrift,

        /// <summary>Adaptive state (goal, mode, policies, memory) diverged.</summary>
        BehaviouralDrift,

        /// <summary>Transition lifecycle behaviour diverged (reserved).</summary>
        LifecycleDrift,

        /// <summary>Execution outcomes diverged (reserved).</summary>
        OutcomeDrift,

        /// <summary>Invariant results diverged (reserved).</summary>
        InvariantDrift
    }
}
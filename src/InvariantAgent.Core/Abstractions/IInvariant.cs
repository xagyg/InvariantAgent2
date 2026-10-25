using InvariantAgent.Core.Model.Control;
using InvariantAgent.Core.Model.Transition;

namespace InvariantAgent.Core.Abstractions
{
    /// <summary>
    /// A behavioural constraint evaluated by the runtime at a specific point in the transition lifecycle.
    /// </summary>
    /// <remarks>
    /// Invariants are the core of IBAAM's Control layer: they define admissible behaviour and are
    /// enforced externally to the (untrusted) planner. Under Hierarchical Invariant Governance (HIG)
    /// each invariant is assigned a <see cref="Layer"/>; under context-aware HIG (HIG-C) it also has a
    /// <see cref="Criticality"/> and a set of operational <see cref="Contexts"/> in which it is most relevant.
    /// </remarks>
    public interface IInvariant
    {
        /// <summary>Unique, human-readable invariant name used in events, reports and override records.</summary>
        string Name { get; }

        /// <summary>The behavioural concern this invariant protects (safety, integrity, identity, self-modification).</summary>
        InvariantCategory Category { get; }

        /// <summary>
        /// The lifecycle checkpoint at which this invariant is evaluated. Invariants with
        /// <see cref="InvariantScope.Transition"/> scope are evaluated at every checkpoint.
        /// </summary>
        InvariantScope Scope { get; }

        /// <summary>
        /// Severity of a violation. <see cref="InvariantSeverity.Error"/> and above remain blocking and
        /// cannot be overridden by default (see <c>OverrideSeverityMetaInvariant</c>).
        /// </summary>
        InvariantSeverity Severity { get; }

        /// <summary>
        /// HIG governance layer. Defaults to <see cref="InvariantLayer.Fundamental"/>, i.e. non-overrideable,
        /// so existing invariants remain hard constraints unless explicitly placed in a lower layer.
        /// </summary>
        InvariantLayer Layer => InvariantLayer.Fundamental;

        /// <summary>
        /// HIG-C criticality: the severity of consequences if this invariant is violated. Used to break
        /// ties between conflicting invariants in the same layer.
        /// </summary>
        InvariantCriticality Criticality => InvariantCriticality.Medium;

        /// <summary>
        /// HIG-C operational contexts in which this invariant is especially relevant. When the current
        /// <see cref="OperationalContext"/> matches, the invariant gains a small priority bonus in
        /// same-layer conflict resolution.
        /// </summary>
        System.Collections.Generic.IReadOnlyList<OperationalContext> Contexts =>
            System.Array.Empty<OperationalContext>();

        /// <summary>Evaluates the invariant against the current transition.</summary>
        /// <param name="context">The transition being governed.</param>
        /// <returns>A pass/fail result with an explanatory reason.</returns>
        InvariantResult Evaluate(TransitionContext context);
    }
}

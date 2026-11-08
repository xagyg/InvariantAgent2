using InvariantAgent.Core.Model.Control;
using InvariantAgent.Core.Model.Transition;
using System.Linq;

namespace InvariantAgent.Core.Abstractions
{
    /// <summary>
    /// A behavioural constraint evaluated by the runtime at a specific point in the transition lifecycle.
    /// </summary>
    /// <remarks>
    /// Invariants are the core of IBAAM's Control layer: they define admissible behaviour and are
    /// enforced externally to the (untrusted) planner. Under Hierarchical and Context-Aware Invariant Governance (HIG-C,
    /// IBAAM 3 revised edition) each invariant has a governance <see cref="Layer"/>, a violation degree
    /// (<see cref="InvariantResult.Degree"/>) and a criticality that depends on the operational context
    /// (<see cref="CriticalityIn"/>). The evaluator compares candidate actions, including declining to act, by their
    /// violation profiles and selects the lexicographically least-bad ones.
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
        /// Severity of a violation. Under the default deployment policy <see cref="InvariantSeverity.Error"/> and above
        /// can never be overridden (see <c>OverrideSeverityMetaInvariant</c>).
        /// </summary>
        InvariantSeverity Severity { get; }

        /// <summary>
        /// HIG governance level. Defaults to <see cref="InvariantLayer.Fundamental"/>, so an invariant nobody classified
        /// can never be violated autonomously.
        /// </summary>
        InvariantLayer Layer => InvariantLayer.Fundamental;

        /// <summary>
        /// Base criticality: the severity of consequences if this invariant is violated in normal operation.
        /// The evaluator uses <see cref="CriticalityIn"/>, which may raise it in particular contexts.
        /// </summary>
        InvariantCriticality Criticality => InvariantCriticality.Medium;

        /// <summary>
        /// Operational contexts in which violating this invariant is more critical than usual. Used by the default
        /// <see cref="CriticalityIn"/>.
        /// </summary>
        System.Collections.Generic.IReadOnlyList<OperationalContext> Contexts =>
            System.Array.Empty<OperationalContext>();

        /// <summary>
        /// Criticality of violating this invariant in the given operational context: C(I, κ).
        /// </summary>
        /// <remarks>
        /// The default raises <see cref="Criticality"/> by one level when <paramref name="context"/> is listed in
        /// <see cref="Contexts"/>. Below the fundamental level the default raise stops at Critical, because only
        /// fundamental invariants may carry Catastrophic criticality (level–criticality consistency, Section 7.3).
        /// Override this for a full criticality table.
        /// </remarks>
        InvariantCriticality CriticalityIn(OperationalContext context)
        {
            if (!Contexts.Contains(context))
            {
                return Criticality;
            }

            var ceiling = Layer == InvariantLayer.Fundamental
                ? InvariantCriticality.Catastrophic
                : InvariantCriticality.Critical;

            return Criticality >= ceiling ? Criticality : Criticality + 1;
        }

        /// <summary>
        /// Tag for the value this invariant protects (IBAAM 6, Section 6.4.1). The conscience reasons from precedent using
        /// these tags ("this resembles a plan blocked for exposing restricted data"). Defaults to a tag derived from
        /// <see cref="Category"/>; override to be more specific.
        /// </summary>
        string ProtectedValue => ProtectedValues.ForCategory(Category);

        /// <summary>Evaluates the invariant against the current transition (the action in flight).</summary>
        /// <param name="context">The transition being governed.</param>
        /// <returns>A result with an explanatory reason and violation degree.</returns>
        InvariantResult Evaluate(TransitionContext context);

        /// <summary>
        /// Evaluates the null action a₀: declining, holding or deferring at this checkpoint instead of continuing.
        /// </summary>
        /// <remarks>
        /// Most invariants constrain what an action does, so declining satisfies them (the default). Invariants that
        /// declining itself breaks, such as a duty to answer the request, report the violation here.
        /// </remarks>
        /// <param name="context">The transition being governed.</param>
        InvariantResult EvaluateDecline(TransitionContext context) => InvariantResult.Allow();
    }
}

using InvariantAgent.Core.Model.Conscience;

namespace InvariantAgent.Core.Abstractions
{
    /// <summary>
    /// Functional conscience (IBAAM 6): weighs what the agent <em>should</em> do inside the boundary the invariants
    /// (Agent Behavioural Controls) set on what it <em>may</em> do.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This contract is deliberately separate from <see cref="IInvariant"/>, so a conscience cannot be registered as
    /// an invariant. An invariant is a fixed, stateless predicate returning pass/fail; a conscience returns graded,
    /// decomposed tension and a stance, is calibrated from governance history, appraises plans against the agent's own
    /// commitments, and feeds back into planning (the four properties of IBAAM 6, Section 4).
    /// </para>
    /// <para>
    /// Containment: the runtime only ever calls the conscience after the relevant invariants have passed, and no stance
    /// has a path to an action an invariant blocks. A Proceed changes nothing downstream.
    /// </para>
    /// </remarks>
    public interface IConscience
    {
        /// <summary>Implementation name recorded on each appraisal.</summary>
        string Name { get; }

        /// <summary>Appraises a plan (or answer). Must depend only on <paramref name="input"/>.</summary>
        /// <param name="input">The plan, state, trend, precedents and live commitments.</param>
        /// <returns>Graded tension, its parts, a stance and a justification generated from the parts.</returns>
        ConscienceAppraisal Appraise(ConscienceInput input);
    }

    /// <summary>
    /// Optional language-model value judge, consulted only for plans with no close precedent (IBAAM 6, Section 6.2).
    /// </summary>
    /// <remarks>
    /// Its estimate stands in for violation risk when history has nothing to say. The judge's output is recorded on the
    /// transition with the rest of the appraisal, so replay reproduces the decision even though the judge is not
    /// deterministic.
    /// </remarks>
    public interface IValueJudge
    {
        /// <summary>Estimates the risk that the plan breaches a protected value, or returns <c>null</c> to abstain.</summary>
        /// <param name="input">The appraisal input.</param>
        /// <returns>A violation-risk component, or <c>null</c>.</returns>
        TensionComponent Judge(ConscienceInput input);
    }
}

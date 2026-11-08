using InvariantAgent.Core.Model.Agent;
using InvariantAgent.Core.Model.Control;
using InvariantAgent.Core.Model.Transition;
using System.Collections.Generic;

namespace InvariantAgent.Core.Abstractions
{
    /// <summary>
    /// Resolves a lifecycle checkpoint under HIG-C: evaluates the applicable invariants for each candidate action and
    /// selects a governed outcome.
    /// </summary>
    public interface IInvariantEvaluator
    {
        /// <summary>
        /// Resolves <paramref name="scope"/> for the action in flight. The candidates are that action and the null action
        /// (declining to continue).
        /// </summary>
        /// <param name="context">The transition being governed.</param>
        /// <param name="scope">The lifecycle checkpoint (plan, execution, self-modification, reduction, ...).</param>
        /// <returns>A report containing the governed outcome, blocking violations and authorised overrides.</returns>
        InvariantEvaluationReport Evaluate(TransitionContext context, InvariantScope scope);

        /// <summary>
        /// Resolves <paramref name="scope"/> with additional candidate actions (used at the plan checkpoint when the
        /// planner proposes alternatives). The default ignores the alternatives.
        /// </summary>
        /// <param name="context">The transition being governed.</param>
        /// <param name="scope">The lifecycle checkpoint.</param>
        /// <param name="alternatives">Further candidates, in the adaptive layer's order of preference.</param>
        InvariantEvaluationReport Evaluate(
            TransitionContext context,
            InvariantScope scope,
            IReadOnlyList<AgentAction> alternatives) => Evaluate(context, scope);
    }
}

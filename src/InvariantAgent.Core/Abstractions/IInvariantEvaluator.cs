using InvariantAgent.Core.Model.Control;
using InvariantAgent.Core.Model.Transition;

namespace InvariantAgent.Core.Abstractions
{
    /// <summary>
    /// Evaluates all invariants applicable to a lifecycle scope and resolves permitted overrides.
    /// </summary>
    public interface IInvariantEvaluator
    {
        /// <summary>Evaluates invariants for <paramref name="scope"/> against the given transition.</summary>
        /// <param name="context">The transition being governed.</param>
        /// <param name="scope">The lifecycle checkpoint (plan, execution, self-modification, reduction, ...).</param>
        /// <returns>A report containing unresolved violations and any authorised overrides.</returns>
        InvariantEvaluationReport Evaluate(TransitionContext context, InvariantScope scope);
    }
}
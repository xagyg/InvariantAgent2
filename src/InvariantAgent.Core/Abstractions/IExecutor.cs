using InvariantAgent.Core.Model;
using InvariantAgent.Core.Model.Transition;

namespace InvariantAgent.Core.Abstractions;

/// <summary>
/// Executes the planner's proposed action for a transition (IBAAM Execution layer).
/// </summary>
public interface IExecutor
{
    /// <summary>
    /// Executes <c>context.Transition.ProposedAction</c> and records the outcome and any proposed
    /// self-modification on the transition. Implementations must not mutate agent state.
    /// </summary>
    /// <param name="context">The transition being governed.</param>
    void Execute(TransitionContext context);
}
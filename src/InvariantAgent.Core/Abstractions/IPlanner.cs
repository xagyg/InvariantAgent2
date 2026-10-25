using InvariantAgent.Core.Model.Agent;
using InvariantAgent.Core.Model.Planning;
using InvariantAgent.Core.Model.Transition;
using System.Threading;
using System.Threading.Tasks;

namespace InvariantAgent.Core.Abstractions
{
    /// <summary>
    /// Proposes the next action for the agent (IBAAM Adaptive layer).
    /// </summary>
    /// <remarks>
    /// Planners - including LLM planners - are treated as untrusted cognitive generators. They only
    /// see a bounded projection of runtime state and their output is always subject to invariant
    /// evaluation before anything is executed.
    /// </remarks>
    public interface IPlanner
    {
        /// <summary>Planner identifier recorded on planning events (e.g. "command", "openai", "gemini").</summary>
        public string Name { get; }

        /// <summary>Produces a proposed action from the bounded planner context.</summary>
        /// <param name="context">User input plus the projected state <c>π(Sₜ)</c>.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The proposed action (capability and input).</returns>
        Task<AgentAction> PlanAsync(PlannerContext context, CancellationToken ct = default);
    }
}
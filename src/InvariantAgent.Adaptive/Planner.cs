using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Agent;
using InvariantAgent.Core.Model.Planning;

namespace InvariantAgent.Adaptive
{
    /// <summary>
    /// Base class for planners (IBAAM Adaptive layer). Wraps synchronous plan generation and converts
    /// planner exceptions into an error action instead of crashing the runtime.
    /// </summary>
    /// <remarks>
    /// A failed plan yields capability <c>"planner"</c>, which is not a registered capability, so the
    /// transition is rejected by <c>AllowedCapabilityInvariant</c> during plan validation.
    /// </remarks>
    public abstract class Planner : IPlanner
    {
        /// <inheritdoc />
        public abstract string Name { get; }

        /// <inheritdoc />
        public Task<AgentAction> PlanAsync(PlannerContext context, CancellationToken ct = default)
        {
            try
            {
                var action = GeneratePlan(context);

                return Task.FromResult(action);
            }
            catch (Exception ex)
            {
                var action = HandlePlannerError(ex, context);

                return Task.FromResult(action);
            }
        }

        /// <summary>Generates the proposed action for the given context.</summary>
        /// <param name="context">Bounded planner context.</param>
        /// <returns>The proposed action.</returns>
        protected abstract AgentAction GeneratePlan(PlannerContext context);

        /// <summary>Converts a planning exception into an error action that governance will reject.</summary>
        /// <param name="ex">The exception thrown by <see cref="GeneratePlan"/>.</param>
        /// <param name="context">The planner context that was being processed.</param>
        protected virtual AgentAction HandlePlannerError(Exception ex, PlannerContext context)
        {
            return new AgentAction
            {
                Capability = "planner",
                Input = $"Planner error: {ex.Message}",
                Error = ex.Message
            };
        }
    }
}
using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Agent;
using InvariantAgent.Core.Model.Planning;

namespace InvariantAgent.Adaptive
{
    public abstract class Planner : IPlanner
    {
        public abstract string Name { get; }

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

        protected abstract AgentAction GeneratePlan(PlannerContext context);

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
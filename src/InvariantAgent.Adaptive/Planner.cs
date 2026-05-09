using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model;

namespace InvariantAgent.Adaptive
{
    public abstract class Planner : IPlanner
    {
        public AgentAction Plan(
            StateProjection state,
            string input)
        {
            try
            {
                return GeneratePlan(state, input);
            }
            catch (Exception ex)
            {
                return HandlePlannerError(
                    ex,
                    state,
                    input);
            }
        }

        protected abstract AgentAction GeneratePlan(
            StateProjection state,
            string input);

        protected virtual AgentAction HandlePlannerError(
            Exception ex,
            StateProjection state,
            string input)
        {
            return new AgentAction
            {
                Tool = "echo",
                Input = $"Planner error: {ex.Message}",
                Error = ex.Message               
            };
        }
    }
}
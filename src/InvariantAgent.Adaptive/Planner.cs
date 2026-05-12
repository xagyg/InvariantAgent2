using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Agent;
using InvariantAgent.Core.Model.Transition;
using InvariantAgent.Core.Pipeline;

namespace InvariantAgent.Adaptive
{
    public abstract class Planner : IPlanner
    {
        public Task PlanAsync(TransitionContext context, CancellationToken ct = default)
        {
            var input = context.Transition.Input;
            var state = context.Transition.Before;

            var projection = StateProjector.Project(state);

            context.Transition.ProposedAction = Plan(projection, input);

            context.Transition.Status = TransitionStatus.Proposed;

            return Task.CompletedTask;
        }

        public AgentAction Plan(StateProjection state, string input)
        {
            try
            {
                return GeneratePlan(state, input);
            }
            catch (Exception ex)
            {
                return HandlePlannerError(ex, state, input);
            }
        }

        protected abstract AgentAction GeneratePlan(StateProjection state, string input);

        protected virtual AgentAction HandlePlannerError(
            Exception ex,
            StateProjection state,
            string input)
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
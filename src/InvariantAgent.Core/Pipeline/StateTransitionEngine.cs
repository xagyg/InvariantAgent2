using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model;

namespace InvariantAgent.Core.Pipeline
{
    public class StateTransitionEngine
    {
        private readonly IPlanner _planner;
        private readonly IExecutor _executor;
        private readonly IControlOperator _control;

        public StateTransitionEngine(
            IPlanner planner,
            IExecutor executor,
            IControlOperator control)
        {
            _planner = planner;
            _executor = executor;
            _control = control;
        }

        public AgentState Step(AgentState s, string input)
        {
            var projection = StateProjector.Project(s);

            // Aₜ = f_adapt(Sₜ, Iₜ)
            var action = _planner.Plan(projection, input);

            // A'ₜ = Π_pre(Aₜ)
            var safeAction = _control.ApplyPre(s, action);
            if (safeAction == null)
                return s;

            // Oₜ = f_exec(A'ₜ)
            var outcome = _executor.Execute(safeAction, s);

            // O'ₜ = Π_post(Oₜ)
            var safeOutcome = _control.ApplyPost(s, outcome);
            if (safeOutcome == null)
                return s;

            // Sₜ₊₁ = update(Sₜ, O'ₜ)
            return ApplyUpdate(s, safeOutcome);
        }

        private AgentState ApplyUpdate(AgentState state, AgentOutcome outcome)
        {
            state.Memory["last"] = outcome.Result;
            return state;
        }
    }
}

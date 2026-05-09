using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model;
using InvariantAgent.Core.Pipeline;
using InvariantAgent.Execution.Engine;
using InvariantAgent.Core.Events;
using InvariantAgent.Core.Model.ToolData;

namespace InvariantAgent.Simulation
{
    public class AgentSimulationEngine
    {
        private readonly IPlanner _planner;
        private readonly IPreControl _pre;
        private readonly IPostControl _post;
        private readonly ToolExecutor _executor;
        private readonly IStateReducer _reducer;

        private AgentState _state = new();

        public IReadOnlyList<AgentEvent> Events => _state.Events;

        public AgentSimulationEngine(
            IPlanner planner,
            IPreControl pre,
            IPostControl post,
            ToolExecutor executor,
            IStateReducer reducer)
        {
            _planner = planner;
            _pre = pre;
            _post = post;
            _executor = executor;
            _reducer = reducer;
        }

        public AgentState Run(string input)
        {
            _state.AddEvent(new StepEvent { StepId = Guid.NewGuid() });

            // 0. π(Sₜ)
            var projection = StateProjector.Project(_state);

            // 1. Adaptive (Aₜ = f_adapt(π(Sₜ), Iₜ))
            var action = _planner.Plan(projection, input);

            _state.AddEvent(new PlanEvent { Tool = action.Tool, Input = action.Input });

            if (action.HasError)
            {            
                return _state;
            }

            // 2. Π_pre (Sₜ, Aₜ)
            var pre = _pre.Evaluate(_state, action);

            _state.AddEvent(new PreControlEvent { Allowed = pre.Allowed, Reason = pre.Reason });

            if (!pre.Allowed)
            {
                return _state;
            }

            // 3. Execution
            var outcome = _executor.Execute(action, _state);

            _state.AddEvent(new ExecutionEvent { Tool = outcome.Tool, Result = (outcome.Result.Data as TextData)?.Value });

            // 4. Π_post (Sₜ, Oₜ)
            var post = _post.Evaluate(_state, outcome);

            _state.AddEvent(new PostControlEvent { Allowed = post.Accepted, Reason = post.Reason });

            if (!post.Accepted)
            {
                return _state;
            }

            // 5. Sₜ₊₁ = T(Sₜ, Oₜ)
            _state = _reducer.Reduce(_state, outcome);

            _state.AddEvent(new StateVersionEvent {  Version = _state.Version });

            return _state;
        }
    }
}

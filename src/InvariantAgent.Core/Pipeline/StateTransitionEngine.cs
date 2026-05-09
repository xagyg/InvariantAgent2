using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model;

namespace InvariantAgent.Core.Pipeline
{
    public class StateTransitionEngine
    {
        private readonly IPlanner _planner;
        private readonly IExecutor _executor;
        private readonly IPreControl _pre;
        private readonly IPostControl _post;
        private readonly IStateReducer _reducer;

        public StateTransitionEngine(
            IPlanner planner,
            IExecutor executor,
            IPreControl pre,
            IPostControl post,
            IStateReducer reducer)
        {
            _planner = planner;
            _executor = executor;
            _pre = pre;
            _post = post;
            _reducer = reducer;
        }

        public AgentState Step(AgentState state, string input)
        {
            /*******
            var projection = StateProjector.Project(s);

            state.AddEvent(new StepEvent());

            // 1. Plan
            var action = _planner.Plan(projection, input);

            state.AddEvent(new PlanEvent { Tool = action.Tool, Input = input });

            var safeAction = _pre.Evaluate(state, action);
            if (safeAction == null)
                return state;

            // 2. Execute
            var outcome = _executor.Execute(action, state);

            state.AddEvent(new ExecutionEvent { Tool = outcome.Tool, Result = outcome.Result });

            var safeOutcome = _post.Evaluate(state, outcome);
            if (safeOutcome == null)
                return state;

            // 3. Update
            return _reducer.Reduce(state, safeOutcome);
            *******/
            return null;
        }

        private AgentState ApplyUpdate(AgentState state, AgentOutcome outcome)
        {
            state.Memory["last"] = outcome.Result;
            return state;
        }
    }
}

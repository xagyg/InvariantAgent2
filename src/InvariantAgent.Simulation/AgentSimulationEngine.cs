using InvariantAgent.Core.Control.Pre;
using InvariantAgent.Core.Control.Post;
using InvariantAgent.Adaptive;
using InvariantAgent.Core.Model;
using InvariantAgent.Core.Pipeline;
using InvariantAgent.Execution.Engine;
using InvariantAgent.Core.Events;

namespace InvariantAgent.Simulation
{
    public class AgentSimulationEngine
    {
        private readonly Planner _planner;
        private readonly PreControl _pre;
        private readonly PostControl _post;
        private readonly ToolExecutor _executor;
        private readonly StateReducer _reducer;

        private AgentState _state = new();

        public IReadOnlyList<AgentEvent> Events => _state.Events;

        public AgentSimulationEngine(
            Planner planner,
            PreControl pre,
            PostControl post,
            ToolExecutor executor,
            StateReducer reducer)
        {
            _planner = planner;
            _pre = pre;
            _post = post;
            _executor = executor;
            _reducer = reducer;
        }

        public AgentState Run(string input)
        {
            // Console.WriteLine($"\n[STATE] Version: {_state.Version}");
            _state.AddEvent("Step", $"StepId={Guid.NewGuid()}");

            // 0. π(Sₜ)
            var projection = StateProjector.Project(_state);

            // 1. Adaptive (Aₜ = f_adapt(π(Sₜ), Iₜ))
            var action = _planner.Plan(projection, input);
            
            _state.AddEvent("Plan", $"Tool={action.Tool}, Input={action.Input}");

          //  Console.WriteLine($"[PLAN] Tool={action.Tool}, Input={action.Input}");

            // 2. Π_pre (Sₜ, Aₜ)
            var pre = _pre.Evaluate(_state, action);

            _state.AddEvent("PreControl", pre.Allowed ? "Allowed" : pre.Reason);

          //  Console.WriteLine($"[Π_pre] Allowed={pre.Allowed}");

            if (!pre.Allowed)
            {
                return _state;
            }

            // 3. Execution
            var outcome = _executor.Execute(action, _state);

            _state.AddEvent("Execution", $"Tool={action.Tool}, Result={outcome.Result}");

           // Console.WriteLine($"[EXEC] Tool={action.Tool}, Result={outcome.Result}");

            // 4. Π_post (Sₜ, Oₜ)
            var post = _post.Evaluate(_state, outcome);

            _state.AddEvent("PostControl", post.Accepted ? "Accepted" : post.Reason);

         //   Console.WriteLine($"[Π_post] Accepted={post.Accepted}");

            if (!post.Accepted)
            {
                return _state;
            }

            // 5. Sₜ₊₁ = T(Sₜ, Oₜ)
            _state = _reducer.Reduce(_state, outcome);

            _state.AddEvent("State", $"Version={_state.Version}");

            return _state;
        }
    }
}

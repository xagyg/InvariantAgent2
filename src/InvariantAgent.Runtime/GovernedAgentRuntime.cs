using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Agent;
using InvariantAgent.Core.Model.Transition;
using InvariantAgent.Execution.Engine;

namespace InvariantAgent.Runtime
{
    public sealed class GovernedAgentRuntime
    {
        private readonly IPlanner _planner;
        private readonly IPreControl _pre;
        private readonly IPostControl _post;
        private readonly IExecutor _executor;
        private readonly IStateReducer _reducer;
        private readonly ITransitionStore _store;

        private AgentState _state = new();

        public AgentState State => _state;

        public GovernedAgentRuntime(
            IPlanner planner,
            IPreControl pre,
            IPostControl post,
            IExecutor executor,
            IStateReducer reducer,
            ITransitionStore store)
        {
            _planner = planner;
            _pre = pre;
            _post = post;
            _executor = executor;
            _reducer = reducer;
            _store = store;
        }

        public async Task<TransitionContext> RunAsync(string input)
        {
            var transition = new Transition
            {
                Input = input,
                Before = _state
            };            

            transition.AddEvent(TransitionEventStage.Input, input,
                new()
                {
                    ["RawInput"] = input
                });

            var context = new TransitionContext
            {
                Transition = transition
            };

            // PLAN
            await _planner.PlanAsync(context);

            transition.AddEvent(TransitionEventStage.Planning, $"Capability={transition.ProposedAction?.Capability}",
                new()
                {
                    ["Capability"] = transition.ProposedAction?.Capability,
                    ["Input"] = context.Transition.Input,
                    ["Planner"] = _planner.Name
                    //["Confidence"] = 0.91
                });

            // PRE-CONTROL
            var decision = _pre.Evaluate(context);

            transition.AddEvent(TransitionEventStage.PreControl, decision.Allowed ? "Allowed" : $"Rejected: {decision.Reason}",
                new()
                {
                    ["Allowed"] = decision.Allowed,
                    ["Reason"] = decision.Reason
                });

            if (!decision.Allowed)
            {
                transition.Status = TransitionStatus.Rejected;
                transition.Reason = decision.Reason;

                _store.Append(transition);

                return context;
            }

            // EXECUTION
            _executor.Execute(context);

            transition.AddEvent(TransitionEventStage.Execution, transition.Outcome?.Success == true 
                ? transition.Outcome.Result
                : transition.Outcome?.Error ?? "Unknown",
                new()
                {
                    ["Capability"] = transition.Outcome?.Capability,
                    ["Success"] = transition.Outcome?.Success,
                    ["Error"] = transition.Outcome?.Error ?? ""
                    //["DurationMs"] = 12
                });

            var postDecision = _post.Evaluate(context);

            transition.AddEvent(TransitionEventStage.PostControl, postDecision.Accepted ? "Accepted"
                    : $"Rejected: {postDecision.Reason}",
                    new()
                    {
                        ["Accepted"] = postDecision.Accepted,
                        ["Reason"] = postDecision.Reason
                    });

            if (!postDecision.Accepted)
            {
                transition.Status = TransitionStatus.Rejected;

                transition.Reason = postDecision.Reason;

                _store.Append(transition);

                return context;
            }

            // STATE ASSIMILATION
            _reducer.Apply(context);

            transition.AddEvent(TransitionEventStage.Reduction, $"Version={transition.After?.Version}",
                new()
                {
                    ["BeforeVersion"] = transition.Before?.Version,
                    ["AfterVersion"] = transition.After?.Version
                });

            // COMMIT NEW STATE
            if (transition.After != null)
            {
                _state = transition.After;
            }

            _store.Append(transition);

            return context;
        }
    }
}
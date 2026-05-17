using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Control;
using InvariantAgent.Core.Model.Agent;
using InvariantAgent.Core.Model.Control;
using InvariantAgent.Core.Model.Transition;
using InvariantAgent.Core.Pipeline;
using InvariantAgent.Core.Transitioning;

namespace InvariantAgent.Runtime
{
    public sealed class GovernedAgentRuntime
    {
        private readonly IPlanner _planner;
        private readonly IInvariantEvaluator _evaluator;
        private readonly IExecutor _executor;
        private readonly IStateReducer _reducer;
        private readonly ITransitionStore _store;

        private AgentState _state = new();

        public AgentState State => _state;

        public GovernedAgentRuntime(
            IPlanner planner,
            IInvariantEvaluator invariantEvaluator,
            IExecutor executor,
            IStateReducer reducer,
            ITransitionStore store)
        {
            _planner = planner;
            _evaluator = invariantEvaluator;
            _executor = executor;
            _reducer = reducer;
            _store = store;
        }

        public async Task<TransitionContext> RunAsync(string input)
        {
            var transition = new Transition
            {
                Input = input,
                Before = _state,
            };

            TransitionPhases.MoveTo(transition, TransitionPhase.InputReceived);

            transition.AddEvent(TransitionEventStage.Input, input,
                new()
                {
                    ["RawInput"] = input
                });

            var context = new TransitionContext
            {
                Transition = transition
            };

            TransitionPhases.MoveTo(transition, TransitionPhase.Planning);

            // PLAN
            var plannerContext = PlannerContextProjector.Project(_state, input);

            var proposedAction = await _planner.PlanAsync(plannerContext, CancellationToken.None);                

            transition.ProposedAction = proposedAction;
            transition.Status = TransitionStatus.Proposed;

            transition.AddEvent(TransitionEventStage.Planning, $"Capability={transition.ProposedAction?.Capability}",
                new()
                {
                    ["Capability"] = transition.ProposedAction?.Capability,
                    ["Input"] = context.Transition.Input,
                    ["Planner"] = _planner.Name
                    //["Confidence"] = 0.91
                });

            TransitionPhases.MoveTo(transition, TransitionPhase.PlanValidation);

            // PRE-CONTROL
            var decision = _evaluator.Evaluate(context, Core.Model.Control.InvariantScope.Plan);

            if (!TryApplyGovernanceOutcome(context, decision)) 
            {
                TransitionPhases.Reject(transition, decision.Summary);
                _store.Append(transition);
                return context;
            }

            TransitionPhases.MoveTo(transition, TransitionPhase.Execution);

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

            TransitionPhases.MoveTo(transition, TransitionPhase.ExecutionValidation);

            // POST-CONTROL
            decision = _evaluator.Evaluate(context, InvariantScope.Execution);

            if (!TryApplyGovernanceOutcome(context, decision))
            {
                TransitionPhases.Reject(transition, decision.Summary);
                _store.Append(transition);
                return context;
            }

            TransitionPhases.MoveTo(transition, TransitionPhase.SelfModificationValidation);

            // SELF-MODIFICATION CHECK
            decision = _evaluator.Evaluate(context, InvariantScope.SelfModification);

            if (!TryApplyGovernanceOutcome(context, decision))
            {
                TransitionPhases.Reject(transition, decision.Summary);
                _store.Append(transition);
                return context;
            }

            // Do not allow reducer to run if transition rejected
            if (context.Transition.Status == TransitionStatus.Rejected)
            {
                _store.Append(transition);
                return context;
            }            

            TransitionPhases.MoveTo(transition, TransitionPhase.Reduction);

            // STATE ASSIMILATION
            _reducer.Apply(context);

            transition.AddEvent(TransitionEventStage.Reduction, $"ProposedVersion={transition.After?.Version}",
                new()
                {
                    ["BeforeVersion"] = transition.Before?.Version,
                    ["ProposedAfterVersion"] = transition.After?.Version,
                    ["Committed"] = false
                });

            // REDUCTION INVARIANTS
            decision = _evaluator.Evaluate(context, InvariantScope.Reduction);

            if (!TryApplyGovernanceOutcome(context, decision))
            {
                TransitionPhases.Reject(transition, decision.Summary);

                _store.Append(transition);

                return context;
            }

            // COMMIT NEW STATE
            if (transition.After != null)
            {
                _state = transition.After;

                transition.AddEvent(TransitionEventStage.Lifecycle, $"CommittedVersion={_state.Version}",
                    new()
                    {
                        ["CommittedVersion"] = _state.Version,
                        ["Committed"] = true
                    });
            }

            TransitionPhases.MoveTo(transition, TransitionPhase.Completed);

            _store.Append(transition);

            return context;
        }

        private bool TryApplyGovernanceOutcome(TransitionContext context, InvariantEvaluationReport report)
        {
            var transition = context.Transition;

            transition.AddEvent(
                TransitionEventStage.Control,
                report.Passed
                    ? $"{report.Scope} invariants passed"
                    : $"{report.Scope} invariants failed: {report.Summary}",
                new Dictionary<string, object>
                {
                    ["Scope"] = report.Scope.ToString(),
                    ["Allowed"] = report.Passed,
                    ["Reason"] = report.Summary,
                    ["ViolationCount"] = report.Violations.Count,
                    ["Violations"] = report.Violations
                        .Select(v => new
                        {
                            v.Invariant,
                            Category = v.Category.ToString(),
                            Scope = v.Scope.ToString(),
                            Severity = v.Severity.ToString(),
                            v.Reason
                        })
                        .ToArray()
                });

            return report.Passed;           
        }
    }
}

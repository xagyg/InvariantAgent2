using InvariantAgent.Conscience;
using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Control;
using InvariantAgent.Core.Model.Agent;
using InvariantAgent.Core.Model.Conscience;
using InvariantAgent.Core.Model.Control;
using InvariantAgent.Core.Model.Drift;
using InvariantAgent.Core.Model.Transition;
using InvariantAgent.Core.Pipeline;
using InvariantAgent.Core.Transitioning;

namespace InvariantAgent.Runtime
{
    /// <summary>
    /// The governed agent runtime: orchestrates every input through the invariant-bounded transition lifecycle.
    /// </summary>
    /// <remarks>
    /// <para>Runtime hierarchy: invariants, then runtime governance, then planner, then capabilities.</para>
    /// <para>Pipeline for each input:</para>
    /// <list type="number">
    /// <item>Input received and the planner proposes an action from the bounded projection <c>π(Sₜ)</c>.</item>
    /// <item>Pre-control: HIG-C resolves the plan checkpoint over the proposal, any planner alternatives
    /// (<see cref="IAlternativePlanner"/>) and declining; the governed action is carried forward.</item>
    /// <item>Conscience plan appraisal (IBAAM 6), when a conscience is configured: Proceed, Revise (re-plan, re-check
    /// plan invariants, re-appraise; bounded), Escalate or Veto.</item>
    /// <item>Capability execution.</item>
    /// <item>Post-control: Execution invariants.</item>
    /// <item>Self-modification invariants.</item>
    /// <item>Reduction: the reducer proposes <c>Sₜ₊₁</c>, then Reduction invariants (e.g. drift bound) are checked.</item>
    /// <item>Conscience answer appraisal: Proceed, caveat (Revise), Escalate or Veto before commit.</item>
    /// <item>Commit the new state and persist the transition.</item>
    /// </list>
    /// <para>
    /// The conscience sits strictly inside the invariants: it is only consulted after they pass, and none of its stances
    /// can authorise anything they block. Every revised plan passes every plan invariant again.
    /// </para>
    /// <para>
    /// At every checkpoint the governed outcome is either to continue (possibly with witnessed overrides) or to decline.
    /// Declining rejects the transition; when every candidate violates a fundamental invariant the transition is also
    /// marked as escalated for human review. Rejected transitions are persisted too, but never change runtime state.
    /// </para>
    /// </remarks>
    public sealed class GovernedAgentRuntime
    {
        private readonly IPlanner _planner;
        private readonly IInvariantEvaluator _evaluator;
        private readonly IExecutor _executor;
        private readonly IStateReducer _reducer;
        private readonly ITransitionStore _store;
        private readonly IDriftBaselineStore? _baselines;
        private readonly ConscienceCoordinator? _conscience;
        private readonly OperationalContextState? _operationalContext;

        // The last committed state. Replaced only after all governance checkpoints pass.
        private AgentState _state;

        /// <summary>The last committed agent state.</summary>
        public AgentState State => _state;

        /// <summary>Creates the runtime from its governed components.</summary>
        /// <param name="planner">Untrusted planner that proposes actions.</param>
        /// <param name="invariantEvaluator">Evaluator enforcing invariants at each checkpoint.</param>
        /// <param name="executor">Executes approved actions.</param>
        /// <param name="reducer">Assimilates approved outcomes into a proposed next state.</param>
        /// <param name="store">Persists every transition, completed or rejected.</param>
        /// <param name="baselines">
        /// Approved drift baselines. When supplied, the baseline in force is recorded on each transition before
        /// reduction invariants run, so replay can score drift against the same baseline.
        /// </param>
        /// <param name="conscience">
        /// Functional conscience (IBAAM 6). When <c>null</c> the runtime behaves exactly as without one.
        /// </param>
        /// <param name="operationalContext">
        /// HIG-C operational context in force (IBAAM 3 revised). Stamped on each transition when it starts, so conflicts
        /// are resolved under it and replay reproduces the decision. When <c>null</c> every transition runs as Normal.
        /// </param>
        /// <remarks>
        /// With a persistent store the runtime resumes from the last committed state, so a restart does not
        /// silently reset the agent while its history survives.
        /// </remarks>
        public GovernedAgentRuntime(
            IPlanner planner,
            IInvariantEvaluator invariantEvaluator,
            IExecutor executor,
            IStateReducer reducer,
            ITransitionStore store,
            IDriftBaselineStore? baselines = null,
            ConscienceCoordinator? conscience = null,
            OperationalContextState? operationalContext = null)
        {
            _planner = planner;
            _evaluator = invariantEvaluator;
            _executor = executor;
            _reducer = reducer;
            _store = store;
            _baselines = baselines;
            _conscience = conscience;
            _operationalContext = operationalContext;

            _state = RestoreCommittedState(store);
        }

        // The After state of the most recent completed transition is the last state that was committed.
        private static AgentState RestoreCommittedState(ITransitionStore store)
        {
            return store.GetAll()
                .LastOrDefault(t => t.Status == TransitionStatus.Completed && t.After != null)
                ?.After ?? new AgentState();
        }

        /// <summary>Processes one input through the full governed transition lifecycle.</summary>
        /// <param name="input">Raw user input.</param>
        /// <returns>The transition context, including the full event trail and final status.</returns>
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
                Transition = transition,
                // HIG-C: the context in force when the transition starts governs all of its checkpoints.
                OperationalContext = _operationalContext?.Current ?? OperationalContext.Normal
            };

            if (context.OperationalContext != OperationalContext.Normal)
            {
                transition.AddEvent(TransitionEventStage.Lifecycle, $"OperationalContext={context.OperationalContext}",
                    new()
                    {
                        ["OperationalContext"] = context.OperationalContext.ToString(),
                        ["Reason"] = _operationalContext?.Reason ?? ""
                    });
            }

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

            // PRE-CONTROL: HIG-C resolves over the proposal, any planner alternatives and declining.
            var alternatives = await ProposeAlternativesAsync(plannerContext, proposedAction);
            var decision = _evaluator.Evaluate(context, InvariantScope.Plan, alternatives);

            if (!TryApplyGovernanceOutcome(context, decision)) 
            {
                RejectByControls(transition, decision);
                _store.Append(transition);
                return context;
            }

            // CONSCIENCE: plan appraisal, inside the boundary the plan invariants just confirmed.
            if (_conscience != null && !await AppraisePlanAsync(context, input))
            {
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
                RejectByControls(transition, decision);
                _store.Append(transition);
                return context;
            }

            TransitionPhases.MoveTo(transition, TransitionPhase.SelfModificationValidation);

            // SELF-MODIFICATION CHECK
            decision = _evaluator.Evaluate(context, InvariantScope.SelfModification);

            if (!TryApplyGovernanceOutcome(context, decision))
            {
                RejectByControls(transition, decision);
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

            // Record the drift baseline these invariants are about to use, for deterministic replay.
            // Without a baseline store we cannot know which baseline the detector uses, so record nothing.
            if (_baselines != null)
            {
                transition.DriftBaseline = DriftBaselineSnapshot.From(_baselines.Current);
            }

            // REDUCTION INVARIANTS
            decision = _evaluator.Evaluate(context, InvariantScope.Reduction);

            if (!TryApplyGovernanceOutcome(context, decision))
            {
                RejectByControls(transition, decision);

                _store.Append(transition);

                return context;
            }

            // CONSCIENCE: answer appraisal, after every control has passed and before anything is committed.
            if (_conscience != null && !AppraiseAnswer(context))
            {
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

        /// <summary>
        /// Plan appraisal loop (IBAAM 6, Section 6.3). Returns <c>false</c> if the transition was stopped.
        /// </summary>
        /// <remarks>
        /// Revise hands the appraisal back to the planner for a bounded number of re-plans. Each revised plan goes through
        /// every plan invariant again and is re-appraised, so the action appraised is always the action executed. When the
        /// revision limit is reached, or the planner cannot produce a different plan, the stance becomes Escalate.
        /// </remarks>
        private async Task<bool> AppraisePlanAsync(TransitionContext context, string input)
        {
            var transition = context.Transition;
            var conscience = _conscience!;

            for (var attempt = 0; ; attempt++)
            {
                var action = transition.ProposedAction;

                if (!conscience.ShouldAppraise(action))
                {
                    return true;
                }

                var appraisal = conscience.Appraise(context, AppraisalPoint.Plan, attempt);

                switch (appraisal.Stance)
                {
                    case ConscienceStance.Proceed:
                        conscience.Record(context, AppraisalPoint.Plan, attempt, action, appraisal, ConscienceEffect.Proceeded);
                        return true;

                    case ConscienceStance.Veto:
                        conscience.Record(context, AppraisalPoint.Plan, attempt, action, appraisal, ConscienceEffect.Vetoed);
                        TransitionPhases.Reject(transition, $"Conscience veto: {appraisal.Justification}");
                        return false;

                    case ConscienceStance.Escalate:
                        Escalate(context, AppraisalPoint.Plan, attempt, action, appraisal, "");
                        return false;
                }

                // REVISE
                if (attempt >= conscience.Options.MaxRevisions)
                {
                    Escalate(context, AppraisalPoint.Plan, attempt, action, appraisal,
                        $"revision limit {conscience.Options.MaxRevisions} reached");
                    return false;
                }

                var revision = ConscienceCoordinator.ToRevision(attempt + 1, action, appraisal);
                var plannerContext = PlannerContextProjector.Project(_state, input, revision);
                var revised = await _planner.PlanAsync(plannerContext, CancellationToken.None);

                if (ConscienceCoordinator.SamePlan(action, revised))
                {
                    Escalate(context, AppraisalPoint.Plan, attempt, action, appraisal,
                        $"planner '{_planner.Name}' could not produce a different plan");
                    return false;
                }

                conscience.Record(context, AppraisalPoint.Plan, attempt, action, appraisal, ConscienceEffect.Revised);

                transition.ProposedAction = revised;

                transition.AddEvent(TransitionEventStage.Planning, $"Revised: Capability={revised?.Capability}",
                    new()
                    {
                        ["Capability"] = revised?.Capability,
                        ["Input"] = revised?.Input,
                        ["Planner"] = _planner.Name,
                        ["RevisionAttempt"] = attempt + 1,
                        ["PreviousCapability"] = action?.Capability,
                        ["PreviousInput"] = action?.Input
                    });

                // Every revised plan passes every plan invariant again.
                var decision = _evaluator.Evaluate(context, InvariantScope.Plan);

                if (!TryApplyGovernanceOutcome(context, decision))
                {
                    RejectByControls(transition, decision);
                    return false;
                }
            }
        }

        /// <summary>
        /// Answer appraisal. Returns <c>false</c> if the transition was stopped before commit. Revise at this point cannot
        /// re-plan an action that has already run, so it is recorded as a caveat on the answer.
        /// </summary>
        private bool AppraiseAnswer(TransitionContext context)
        {
            var transition = context.Transition;
            var conscience = _conscience!;
            var action = transition.ProposedAction;

            if (!conscience.Options.AppraiseAnswers || !conscience.ShouldAppraise(action))
            {
                return true;
            }

            var attempt = transition.Appraisals.Count(a => a.Point == AppraisalPoint.Plan) - 1;
            var appraisal = conscience.Appraise(context, AppraisalPoint.Answer, Math.Max(0, attempt));

            switch (appraisal.Stance)
            {
                case ConscienceStance.Revise:
                    conscience.Record(context, AppraisalPoint.Answer, Math.Max(0, attempt), action, appraisal,
                        ConscienceEffect.Caveated, "answer already produced; caveat recorded instead of re-planning");
                    return true;

                case ConscienceStance.Escalate:
                    Escalate(context, AppraisalPoint.Answer, Math.Max(0, attempt), action, appraisal, "");
                    return false;

                case ConscienceStance.Veto:
                    conscience.Record(context, AppraisalPoint.Answer, Math.Max(0, attempt), action, appraisal,
                        ConscienceEffect.Vetoed);
                    TransitionPhases.Reject(transition, $"Conscience veto: {appraisal.Justification}");
                    return false;

                default:
                    conscience.Record(context, AppraisalPoint.Answer, Math.Max(0, attempt), action, appraisal,
                        ConscienceEffect.Proceeded);
                    return true;
            }
        }

        // Holds the transition for human review: nothing further runs and nothing is committed.
        private void Escalate(
            TransitionContext context,
            AppraisalPoint point,
            int attempt,
            AgentAction action,
            ConscienceAppraisal appraisal,
            string note)
        {
            _conscience!.Record(context, point, attempt, action, appraisal, ConscienceEffect.Escalated, note);

            TransitionPhases.Reject(context.Transition,
                $"Conscience escalated for human review (conscience-review {context.Transition.Id.ToString("N")[..8]} uphold|overturn): " +
                appraisal.Justification);
        }

        // Rejects through the controls and records which invariants blocked it (boundary labels for the conscience).
        // A fundamental conflict that no pre-authorised protocol covers is held for human review (HIG-C, Section 7.2).
        private static void RejectByControls(Transition transition, InvariantEvaluationReport report)
        {
            transition.BlockingInvariants = report.Violations
                .Select(v => v.Invariant)
                .Distinct()
                .ToList();

            if (report.RequiresEscalation)
            {
                transition.GovernanceEscalated = true;
            }

            TransitionPhases.Reject(transition, report.Summary);
        }

        // Asks an alternative-capable planner for further candidates; other planners contribute only their proposal.
        private async Task<IReadOnlyList<AgentAction>> ProposeAlternativesAsync(
            Core.Model.Planning.PlannerContext plannerContext,
            AgentAction proposed)
        {
            if (_planner is not IAlternativePlanner alternativePlanner || proposed == null)
            {
                return Array.Empty<AgentAction>();
            }

            var alternatives = await alternativePlanner.ProposeAlternativesAsync(
                plannerContext, proposed, CancellationToken.None);

            return alternatives?
                .Where(a => a != null && !ConscienceCoordinator.SamePlan(proposed, a))
                .ToList() ?? (IReadOnlyList<AgentAction>)Array.Empty<AgentAction>();
        }

        /// <summary>
        /// Records a Control event describing the evaluation report (violations and overrides) and
        /// returns whether the transition may proceed.
        /// </summary>
        private bool TryApplyGovernanceOutcome(TransitionContext context, InvariantEvaluationReport report)
        {
            var transition = context.Transition;

            // HIG-C may select a planner alternative over the proposal: from here on the transition carries that action.
            if (report.Passed && report.SelectedAlternative != null)
            {
                var previous = transition.ProposedAction;
                transition.ProposedAction = report.SelectedAlternative;

                transition.AddEvent(TransitionEventStage.Planning,
                    $"Governed alternative selected: Capability={report.SelectedAlternative.Capability}",
                    new()
                    {
                        ["Capability"] = report.SelectedAlternative.Capability,
                        ["Input"] = report.SelectedAlternative.Input,
                        ["Selected"] = report.Resolution?.SelectedId ?? "",
                        ["PreviousCapability"] = previous?.Capability,
                        ["PreviousInput"] = previous?.Input,
                        ["Reason"] = report.Resolution?.Reason ?? ""
                    });
            }

            var conflict = ConflictRecord.From(report);

            if (conflict != null)
            {
                transition.Conflicts.Add(conflict);
            }

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
                    ["OverrideCount"] = report.Overrides.Count,
                    ["Declined"] = report.Declined,
                    ["RequiresEscalation"] = report.RequiresEscalation,
                    ["Selected"] = report.Resolution?.SelectedId ?? GovernanceCandidate.ProposedId,
                    ["GovernedSet"] = report.Resolution?.GovernedSet.ToArray() ?? Array.Empty<string>(),
                    ["Violations"] = report.Violations
                        .Select(v => new
                        {
                            v.Invariant,
                            Category = v.Category.ToString(),
                            Scope = v.Scope.ToString(),
                            Severity = v.Severity.ToString(),
                            Layer = v.Layer.ToString(),
                            Criticality = v.CriticalityInContext.ToString(),
                            v.Degree,
                            v.Reason
                        })
                        .ToArray(),
                    ["Overrides"] = report.Overrides
                        .Select(o => new
                        {
                            OverriddenInvariant = o.OverriddenViolation.Invariant,
                            OverriddenLayer = o.OverriddenLayer.ToString(),
                            o.PreservedHigherPriorityInvariants,
                            Witnesses = o.Witnesses.Select(w => w.ToString()).ToArray(),
                            o.Justification,
                            o.MetaInvariantCategory,
                            o.RequiresReview,
                            o.ReviewReasons
                        })
                        .ToArray()
                });

            return report.Passed;           
        }
    }
}

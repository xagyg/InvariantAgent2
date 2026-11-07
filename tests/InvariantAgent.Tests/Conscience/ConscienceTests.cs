using InvariantAgent.Conscience;
using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Conscience;
using InvariantAgent.Core.Extensions;
using InvariantAgent.Core.Model.Conscience;
using InvariantAgent.Core.Model.Transition;
using InvariantAgent.Core.Replay;
using InvariantAgent.Safety.Invariants.SelfModification;
using InvariantAgent.Storage;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace InvariantAgent.Tests.Conscience;

/// <summary>
/// Tests for functional conscience (IBAAM 6). Grouped by the paper: containment (Section 5), the four properties of the
/// discriminating test (Section 4, checked as in the Section 8 evaluation design), stances (Section 6.3), commitments
/// and review (Section 6.4.2), replay (Section 6.4.4) and persistence.
/// </summary>
public sealed class ConscienceTests
{
    // ---------------------------------------------------------------- contract and containment

    [Fact]
    public void Conscience_HasItsOwnContract_AndCannotBeRegisteredAsAnInvariant()
    {
        Assert.False(typeof(IInvariant).IsAssignableFrom(typeof(HistoryCalibratedConscience)));
        Assert.False(typeof(IInvariant).IsAssignableFrom(typeof(ConscienceCoordinator)));

        var host = ConscienceTestHost.Create();

        Assert.DoesNotContain(host.Provider.GetServices<IInvariant>(), i => i is IConscience);
    }

    [Fact]
    public async Task OrdinaryPlan_OnFreshAgent_ProceedsAtBothAppraisalPoints()
    {
        var host = ConscienceTestHost.Create();

        var context = await host.Runtime.RunAsync("echo hello");

        Assert.Equal(TransitionStatus.Completed, context.Transition.Status);
        Assert.Equal(2, context.Transition.Appraisals.Count);
        Assert.Equal(AppraisalPoint.Plan, context.Transition.Appraisals[0].Point);
        Assert.Equal(AppraisalPoint.Answer, context.Transition.Appraisals[1].Point);
        Assert.All(context.Transition.Appraisals, a => Assert.Equal(ConscienceEffect.Proceeded, a.Effect));
        Assert.Contains(context.Transition.Events, e => e.Stage == TransitionEventStage.Conscience);
    }

    [Fact]
    public async Task Containment_BlockedPlan_IsNeverAppraised()
    {
        var host = ConscienceTestHost.Create();

        var context = await host.Runtime.RunAsync("delete everything");

        // The controls decide first; the conscience only sees plans the controls have let through.
        Assert.Equal(TransitionStatus.Rejected, context.Transition.Status);
        Assert.Contains("NoDeleteInvariant", context.Transition.BlockingInvariants);
        Assert.Empty(context.Transition.Appraisals);
    }

    [Fact]
    public async Task Containment_ProceedDoesNotOverrideALaterControl()
    {
        var host = ConscienceTestHost.Create();

        var context = await host.Runtime.RunAsync("memory-set password=a");

        Assert.Equal(ConscienceEffect.Proceeded, context.Transition.Appraisals.Single().Effect);
        Assert.Equal(TransitionStatus.Rejected, context.Transition.Status);
        Assert.Contains("AllowedMemoryKeyInvariant", context.Transition.BlockingInvariants);
        Assert.Equal(0, host.Runtime.State.Version);
    }

    [Fact]
    public async Task Disabled_RunsWithInvariantsOnly()
    {
        var host = ConscienceTestHost.Create(o => o.Enabled = false);

        await host.Runtime.RunAsync("memory-set password=a");
        var context = await host.Runtime.RunAsync("memory-set password=b");

        Assert.Null(host.Coordinator);
        Assert.Empty(context.Transition.Appraisals);
        Assert.Contains("AllowedMemoryKeyInvariant", context.Transition.BlockingInvariants);
    }

    [Fact]
    public async Task ExemptGovernanceTools_AreNotAppraised()
    {
        var host = ConscienceTestHost.Create();

        var context = await host.Runtime.RunAsync("conscience");

        Assert.Equal(TransitionStatus.Completed, context.Transition.Status);
        Assert.Empty(context.Transition.Appraisals);
    }

    // ---------------------------------------------------------------- property 1: history sensitivity

    [Fact]
    public async Task HistorySensitivity_SamePlan_IsAppraisedDifferentlyAfterABoundaryLabel()
    {
        var fresh = ConscienceTestHost.Create();
        var first = await fresh.Runtime.RunAsync("memory-set password=a");

        var experienced = ConscienceTestHost.Create();
        await experienced.Runtime.RunAsync("memory-set password=x");
        var second = await experienced.Runtime.RunAsync("memory-set password=a");

        var before = first.Transition.Appraisals[0].Appraisal;
        var after = second.Transition.Appraisals[0].Appraisal;

        Assert.Equal(ConscienceStance.Proceed, before.Stance);
        Assert.NotEqual(ConscienceStance.Proceed, after.Stance);
        Assert.True(after.Tension > before.Tension);
        Assert.Contains("AllowedMemoryKeyInvariant", after.Justification);

        // Anticipation: stopped before execution rather than rejected by the control afterwards.
        Assert.True(second.Transition.Appraisals.Last().Stopped);
        Assert.Null(second.Transition.Outcome);
        Assert.Empty(second.Transition.BlockingInvariants);
    }

    [Fact]
    public async Task HistorySensitivity_IsDeterministicForAFixedHistory()
    {
        var host = ConscienceTestHost.Create();
        await host.Runtime.RunAsync("memory-set password=x");

        var context = new TransitionContext
        {
            Transition = new Transition
            {
                Input = "memory-set password=a",
                Before = host.Runtime.State,
                ProposedAction = new() { Capability = "memory-set", Input = "password=a" }
            }
        };

        var a = host.Coordinator!.Appraise(context, AppraisalPoint.Plan, 0);
        var b = host.Coordinator!.Appraise(context, AppraisalPoint.Plan, 0);

        Assert.Equal(a.Tension, b.Tension);
        Assert.Equal(a.Stance, b.Stance);
        Assert.Equal(a.Justification, b.Justification);
    }

    [Fact]
    public async Task HistorySensitivity_PlanScopeRejectionOfAnotherPlan_IsNotEvidence()
    {
        var host = ConscienceTestHost.Create();

        // A typo is rejected by a plan-scope invariant (unknown capability)...
        var typo = await host.Runtime.RunAsync("set-memory goal=\"learn a song\"");
        Assert.Contains("AllowedCapabilityInvariant", typo.Transition.BlockingInvariants);

        // ...which says nothing about the correct command, which has already passed the plan invariants.
        var fixedCommand = await host.Runtime.RunAsync("memory-set goal=\"learn a song\"");

        var plan = fixedCommand.Transition.Appraisals[0];
        Assert.Equal(ConscienceEffect.Proceeded, plan.Effect);
        Assert.Empty(plan.Appraisal.CitedPrecedents);
        Assert.Equal(TransitionStatus.Completed, fixedCommand.Transition.Status);
    }

    [Fact]
    public void TrendPressure_AmplifiesPlanConcerns_ButNeverStopsAPlanOnItsOwn()
    {
        var conscience = new HistoryCalibratedConscience(new ConscienceOptions());
        var critical = new InvariantAgent.Core.Model.Stability.StabilityAssessment
        {
            Region = InvariantAgent.Core.Model.Stability.StabilityRegion.Critical,
            Confidence = InvariantAgent.Core.Model.Stability.MeasurementConfidence.High
        };

        ConscienceInput Input(InvariantAgent.Core.Model.Stability.StabilityAssessment trend, double baseRate) => new(
            AppraisalPoint.Plan,
            new() { Capability = "memory-set", Input = "goal=learn to swim" },
            new(),
            trend,
            Array.Empty<Precedent>(),
            Array.Empty<Commitment>()) { BaseRate = baseRate };

        // No plan-specific concern: Critical history alone does not stop it.
        var quiet = conscience.Appraise(Input(critical, 0));
        Assert.Equal(0, quiet.Tension, 3);
        Assert.Equal(ConscienceStance.Proceed, quiet.Stance);

        // A real concern weighs more when history is unstable.
        var stable = conscience.Appraise(Input(new(), 0.4));
        var unstable = conscience.Appraise(Input(critical, 0.4));
        Assert.True(unstable.Tension > stable.Tension);
        Assert.True(unstable.Tension < 1.0);
    }

    // ---------------------------------------------------------------- property 2: generalisation (leave-one-out)

    // Retention, not leave-one-out: the history was calibrated WITH the control, which is removed only afterwards. This
    // shows the conscience keeps a concern a control taught it, so removing or weakening that control later does not
    // silently reopen it. It does not show generalisation (see the next test).
    [Fact]
    public async Task Retention_FlagsConcernAfterTheControlThatTaughtItIsRemoved()
    {
        // Calibrate with the full control set: the memory-key control produces a boundary label.
        var calibrated = ConscienceTestHost.Create();
        var label = (await calibrated.Runtime.RunAsync("memory-set password=x")).Transition;
        Assert.Contains("AllowedMemoryKeyInvariant", label.BlockingInvariants);

        // Remove the control. With invariants only, nothing represents the concern any more...
        var withoutControl = ConscienceTestHost.Create(o => o.Enabled = false,
            sharedStore: Seeded(label), removeInvariant: typeof(AllowedMemoryKeyInvariant));
        var unguarded = await withoutControl.Runtime.RunAsync("memory-set password=z");
        Assert.Equal(TransitionStatus.Completed, unguarded.Transition.Status);

        // ...but the conscience, calibrated on the same history, still flags a plan resembling the blocked one.
        var leaveOneOut = ConscienceTestHost.Create(
            sharedStore: Seeded(label), removeInvariant: typeof(AllowedMemoryKeyInvariant));

        var flagged = await leaveOneOut.Runtime.RunAsync("memory-set password=z");

        Assert.Equal(TransitionStatus.Rejected, flagged.Transition.Status);
        Assert.Empty(flagged.Transition.BlockingInvariants);
        Assert.True(flagged.Transition.Appraisals.Last().Stopped);
        Assert.Contains("AllowedMemoryKeyInvariant", flagged.Transition.Appraisals[0].Appraisal.Justification);
    }

    // Leave-one-out as IBAAM 6, Section 8 specifies it: calibrate with the control REMOVED, then measure flags on plans
    // that violate it. This records the current v1 result, not the desired one. The deterministic v1 conscience learns
    // only from boundary labels (control outcomes), drift and commitments, so a concern that no control ever labelled is
    // invisible to it: it fails property 2 on this design, exactly as Section 5.2 predicts for a conscience trained on
    // control outcomes alone. When value-learning signals (reviewer audits, corrections, red-team seeds) or an IValueJudge
    // are added, this test should be inverted to assert the flag.
    [Fact]
    public async Task Generalisation_LeaveOneOutAsSpecified_V1DoesNotFlagAConcernNoControlLabelled()
    {
        var host = ConscienceTestHost.Create(removeInvariant: typeof(AllowedMemoryKeyInvariant));

        var calibration = await host.Runtime.RunAsync("memory-set password=x");
        Assert.Equal(TransitionStatus.Completed, calibration.Transition.Status);
        Assert.Empty(calibration.Transition.BlockingInvariants);

        var probe = await host.Runtime.RunAsync("memory-set password=z");

        Assert.Equal(TransitionStatus.Completed, probe.Transition.Status);
        Assert.DoesNotContain(probe.Transition.Appraisals, a => a.Stopped);
        Assert.All(probe.Transition.Appraisals, a => Assert.True(a.Appraisal.Tension < 0.55));
    }

    // ---------------------------------------------------------------- property 3: deliberative influence

    [Fact]
    public async Task DeliberativeInfluence_ReviseFeedsBackIntoPlanning_AndRevisedPlanRepassesControls()
    {
        var planner = new RevisingPlanner();
        var host = ConscienceTestHost.Create(planner: planner);

        await host.Runtime.RunAsync("memory-set password=x");
        var context = await host.Runtime.RunAsync("memory-set password=hunter2");

        var transition = context.Transition;

        Assert.Equal(1, planner.RevisionRequests);
        Assert.Equal(TransitionStatus.Completed, transition.Status);
        Assert.Equal("notes=hunter2", transition.ProposedAction.Input);
        Assert.Equal("hunter2", host.Runtime.State.Memory["notes"]);

        Assert.Equal(ConscienceEffect.Revised, transition.Appraisals[0].Effect);
        Assert.Equal("password=hunter2", transition.Appraisals[0].Input);
        Assert.Equal(ConscienceEffect.Proceeded, transition.Appraisals[1].Effect);
        Assert.Equal(1, transition.Appraisals[1].Attempt);

        // Every revised plan passes every plan invariant again.
        Assert.Equal(2, transition.Events.Count(e =>
            e.Stage == TransitionEventStage.Control &&
            e.Metadata.TryGetValue("Scope", out var scope) && Equals(scope, "Plan")));
    }

    [Fact]
    public async Task DeliberativeInfluence_WithoutFeedback_ThePlanIsHeldInstead()
    {
        var planner = new RevisingPlanner();
        var host = ConscienceTestHost.Create(o => o.MaxRevisions = 0, planner: planner);

        await host.Runtime.RunAsync("memory-set password=x");
        var context = await host.Runtime.RunAsync("memory-set password=hunter2");

        Assert.Equal(0, planner.RevisionRequests);
        Assert.Equal(ConscienceEffect.Escalated, context.Transition.Appraisals.Single().Effect);
        Assert.False(host.Runtime.State.Memory.ContainsKey("notes"));
    }

    [Fact]
    public async Task Revise_AtTheRevisionLimit_BecomesEscalate()
    {
        // Commitment conflict off, to isolate the revision limit (otherwise the agent's own Revise commitment
        // escalates a cosmetic revision sooner).
        var host = ConscienceTestHost.Create(o => o.CommitmentConflictWeight = 0, planner: new StubbornPlanner());

        await host.Runtime.RunAsync("memory-set password=x");
        var context = await host.Runtime.RunAsync("memory-set password=y");

        var appraisals = context.Transition.Appraisals;

        Assert.Equal(3, appraisals.Count);
        Assert.Equal(ConscienceEffect.Revised, appraisals[0].Effect);
        Assert.Equal(ConscienceEffect.Revised, appraisals[1].Effect);
        Assert.Equal(ConscienceEffect.Escalated, appraisals[2].Effect);
        Assert.Contains("revision limit", appraisals[2].Note);
        Assert.Equal(ConscienceStance.Revise, appraisals[2].Appraisal.Stance);
    }

    [Fact]
    public async Task Revise_WithALiteralPlanner_EscalatesBecauseNoDifferentPlanExists()
    {
        var host = ConscienceTestHost.Create();

        await host.Runtime.RunAsync("memory-set password=x");
        var context = await host.Runtime.RunAsync("memory-set password=y");

        var record = context.Transition.Appraisals.Single();

        Assert.Equal(ConscienceStance.Revise, record.Appraisal.Stance);
        Assert.Equal(ConscienceEffect.Escalated, record.Effect);
        Assert.Contains("could not produce a different plan", record.Note);
        Assert.Contains("conscience-review", context.Transition.Reason);
    }

    // ---------------------------------------------------------------- property 4: self-reference (commitments)

    [Fact]
    public async Task SelfReference_SeededReviewerCommitment_VetoesConsistentlyWithIt()
    {
        var commitments = new InMemoryCommitmentStore();
        commitments.Append(Seed("search", "competitor salary data", CommitmentKind.Declined));

        var host = ConscienceTestHost.Create(sharedCommitments: commitments);

        var inconsistent = await host.Runtime.RunAsync("search competitor salary data");
        var consistent = await host.Runtime.RunAsync("search weather in brisbane");

        // No invariant names this concern: only the commitment does.
        Assert.Equal(TransitionStatus.Rejected, inconsistent.Transition.Status);
        Assert.Empty(inconsistent.Transition.BlockingInvariants);
        Assert.Equal(ConscienceStance.Veto, inconsistent.Transition.Appraisals.Single().Appraisal.Stance);
        Assert.Single(inconsistent.Transition.Appraisals.Single().Appraisal.CitedCommitments);

        Assert.Equal(TransitionStatus.Completed, consistent.Transition.Status);
    }

    [Fact]
    public async Task SelfReference_AgentCommitmentFromAnEscalation_ShapesTheNextAppraisal()
    {
        var host = ConscienceTestHost.Create();

        await host.Runtime.RunAsync("memory-set password=x");
        var escalated = await host.Runtime.RunAsync("memory-set password=y");

        var commitment = Assert.Single(host.Commitments.GetAll());
        Assert.Equal(CommitmentOrigin.Agent, commitment.Origin);
        Assert.Equal(escalated.Transition.Id, commitment.SourceTransition);
        Assert.Equal("memory-integrity", commitment.Value);
        Assert.Equal(CommitmentScope.User, commitment.Scope);

        var again = await host.Runtime.RunAsync("memory-set password=y");

        var conflict = again.Transition.Appraisals[0].Appraisal.Parts.Single(p => p.Kind == TensionKind.CommitmentConflict);
        Assert.True(conflict.Value > 0.5);
        Assert.Contains(commitment.Id, again.Transition.Appraisals[0].Appraisal.CitedCommitments);
        Assert.True(again.Transition.Appraisals[0].Appraisal.Tension >
                    escalated.Transition.Appraisals[0].Appraisal.Tension);
    }

    [Fact]
    public async Task SelfReference_CommitmentWeighsTheSameConduct_NotMerelySimilarConduct()
    {
        var commitments = new InMemoryCommitmentStore();
        commitments.Append(Seed("memory-set", "goal=\"learn a song\"", CommitmentKind.Declined, CommitmentOrigin.Agent));

        var host = ConscienceTestHost.Create(sharedCommitments: commitments);

        // Same capability and key, different goal: barely above the match threshold, so it barely weighs.
        var different = await host.Runtime.RunAsync("memory-set goal=\"learn the piano\"");
        var conflict = different.Transition.Appraisals[0].Appraisal.Parts.Single(p => p.Kind == TensionKind.CommitmentConflict);
        Assert.True(conflict.Value < 0.2, $"conflict {conflict.Value}");
        Assert.Equal(TransitionStatus.Completed, different.Transition.Status);

        // The conduct the commitment declined weighs in full.
        var same = await host.Runtime.RunAsync("memory-set goal=\"learn a song\"");
        Assert.Equal(0.8, same.Transition.Appraisals[0].Appraisal.Parts.Single(p => p.Kind == TensionKind.CommitmentConflict).Value, 2);
    }

    // ---------------------------------------------------------------- review of escalations

    [Fact]
    public async Task Review_Overturn_ClearsSimilarPlans_WhichThenMeetTheControls()
    {
        var host = ConscienceTestHost.Create();

        await host.Runtime.RunAsync("memory-set password=x");
        var escalated = await host.Runtime.RunAsync("memory-set password=y");
        var id = escalated.Transition.Id.ToString("N")[..8];

        var review = await host.Runtime.RunAsync($"conscience-review {id} overturn tested in staging");
        Assert.Equal(TransitionStatus.Completed, review.Transition.Status);

        var retry = await host.Runtime.RunAsync("memory-set password=y");

        // The reviewer's decision takes precedence over the agent's judgement...
        Assert.Equal(ConscienceEffect.Proceeded, retry.Transition.Appraisals[0].Effect);
        Assert.Contains("reviewer cleared", retry.Transition.Appraisals[0].Appraisal.Justification, StringComparison.OrdinalIgnoreCase);
        // ...but never over the controls: the memory-key invariant still rejects it.
        Assert.Contains("AllowedMemoryKeyInvariant", retry.Transition.BlockingInvariants);
    }

    [Fact]
    public async Task Review_Uphold_TurnsTheNextSimilarPlanIntoAVeto()
    {
        var host = ConscienceTestHost.Create();

        await host.Runtime.RunAsync("memory-set password=x");
        var escalated = await host.Runtime.RunAsync("memory-set password=y");
        await host.Runtime.RunAsync($"conscience-review {escalated.Transition.Id.ToString("N")[..8]} uphold");

        var retry = await host.Runtime.RunAsync("memory-set password=y");

        Assert.Equal(ConscienceEffect.Vetoed, retry.Transition.Appraisals.Single().Effect);
        Assert.StartsWith("Conscience veto", retry.Transition.Reason);

        var reviewer = host.Commitments.GetAll().Single(c => c.Origin == CommitmentOrigin.Reviewer);
        Assert.Equal(escalated.Transition.Id, reviewer.Resolves);

        // The reviewer decision superseded the agent's own commitment for that escalation.
        var live = host.Coordinator!.LiveCommitments();
        Assert.DoesNotContain(live, c => c.Origin == CommitmentOrigin.Agent && c.SourceTransition == escalated.Transition.Id);
    }

    [Fact]
    public async Task Review_CannotResolveTheSameEscalationTwice()
    {
        var host = ConscienceTestHost.Create();

        await host.Runtime.RunAsync("memory-set password=x");
        var escalated = await host.Runtime.RunAsync("memory-set password=y");
        var id = escalated.Transition.Id.ToString("N")[..8];

        await host.Runtime.RunAsync($"conscience-review {id} uphold");
        var second = await host.Runtime.RunAsync($"conscience-review {id} overturn");

        Assert.Equal(TransitionStatus.Rejected, second.Transition.Status);
        Assert.Single(host.Commitments.GetAll(), c => c.Origin == CommitmentOrigin.Reviewer);
    }

    // ---------------------------------------------------------------- answer appraisal

    [Fact]
    public async Task AnswerAppraisal_ReviseBecomesACaveat_AndTheAnswerIsCommitted()
    {
        // Violation risk off (it does not apply at the answer point anyway) and a low threshold, so an ordinary goal
        // change (drift 40 of 70) proceeds at the plan point but produces Revise at the answer point.
        var host = ConscienceTestHost.Create(o =>
        {
            o.ViolationRiskWeight = 0;
            o.ReviseThreshold = 0.2;
        });

        var context = await host.Runtime.RunAsync("memory-set goal=new objective");

        var answer = context.Transition.Appraisals.Single(a => a.Point == AppraisalPoint.Answer);

        Assert.Equal(ConscienceEffect.Caveated, answer.Effect);
        Assert.True(answer.Appraisal.Parts.Single(p => p.Kind == TensionKind.ForecastDrift).Value > 0);
        Assert.False(answer.Appraisal.Parts.Single(p => p.Kind == TensionKind.ViolationRisk).Applicable);
        Assert.Equal(TransitionStatus.Completed, context.Transition.Status);
        Assert.Equal("new objective", host.Runtime.State.Memory["goal"]);
    }

    [Fact]
    public async Task AnswerAppraisal_Veto_StopsTheCommit()
    {
        var host = ConscienceTestHost.Create(o =>
        {
            o.ViolationRiskWeight = 0;
            o.ReviseThreshold = 0.1;
            o.EscalateThreshold = 0.15;
            o.VetoThreshold = 0.2;
        });

        var context = await host.Runtime.RunAsync("memory-set goal=new objective");

        Assert.Equal(TransitionStatus.Rejected, context.Transition.Status);
        Assert.Equal(AppraisalPoint.Answer, context.Transition.Appraisals.Last().Point);
        Assert.NotNull(context.Transition.Outcome);
        Assert.Equal(0, host.Runtime.State.Version);
    }

    // ---------------------------------------------------------------- replay, calibration, persistence

    [Fact]
    public async Task Replay_ConscienceStop_ReproducesTheLiveDecisionFromTheRecordedAppraisal()
    {
        var host = ConscienceTestHost.Create();

        await host.Runtime.RunAsync("echo hello");
        await host.Runtime.RunAsync("memory-set password=x");
        await host.Runtime.RunAsync("memory-set password=y");

        var validator = host.Provider.GetRequiredService<ReplayValidator>();

        foreach (var transition in host.Store.GetAll())
        {
            var result = validator.Validate(new TransitionContext { Transition = transition.Clone() });

            Assert.True(result.Passed, $"Replay mismatch for '{transition.Input}'");
        }

        var stopped = host.Store.GetAll().Last();
        var stoppedResult = validator.Validate(new TransitionContext { Transition = stopped.Clone() });
        Assert.True(stoppedResult.Comparison.ConscienceStopRecorded);
    }

    [Fact]
    public async Task Calibration_ScoresViolationRiskAgainstRealisedControlOutcomes()
    {
        var host = ConscienceTestHost.Create();

        await host.Runtime.RunAsync("echo one");
        await host.Runtime.RunAsync("memory-set password=x");
        await host.Runtime.RunAsync("calc 1+1");
        await host.Runtime.RunAsync("memory-set password=y");

        var report = CalibrationReport.Compute(host.Store.GetAll());

        Assert.Equal(3, report.Count);
        Assert.Equal(1, report.Unlabelled);
        Assert.NotNull(report.BrierScore);
        Assert.InRange(report.BrierScore!.Value, 0.0, 1.0);
    }

    [Fact]
    public async Task Persistence_AppraisalsAndCommitmentsSurviveARestart_ConversationScopeDoesNot()
    {
        var directory = Path.Combine(Path.GetTempPath(), "ia2-conscience-" + Guid.NewGuid().ToString("N"));

        try
        {
            var first = ConscienceTestHost.Create(dataDirectory: directory, planner: new RevisingPlanner());
            await first.Runtime.RunAsync("memory-set password=x");
            await first.Runtime.RunAsync("memory-set password=y"); // Revise -> conversation-scoped commitment
            first.Provider.Dispose();

            var second = ConscienceTestHost.Create(dataDirectory: directory);
            await second.Runtime.RunAsync("memory-set password=z"); // Escalate -> user-scoped commitment
            second.Provider.Dispose();

            var restarted = ConscienceTestHost.Create(dataDirectory: directory);

            var stored = restarted.Store.GetAll();
            var revised = stored[1].Appraisals[0];
            Assert.Equal(ConscienceEffect.Revised, revised.Effect);
            Assert.Equal(4, revised.Appraisal.Parts.Count);
            Assert.False(string.IsNullOrWhiteSpace(revised.Appraisal.Justification));
            Assert.IsType<FileCommitmentStore>(restarted.Commitments);
            Assert.Equal(2, restarted.Commitments.GetAll().Count);

            var live = restarted.Coordinator!.LiveCommitments();
            Assert.Single(live);
            Assert.Equal(CommitmentScope.User, live[0].Scope);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Fact]
    public void Ledger_ExpiredAndOutOfScopeCommitments_AreNotLive()
    {
        var now = DateTimeOffset.UtcNow;

        var all = new[]
        {
            Seed("echo", "a", CommitmentKind.Declined, CommitmentOrigin.Agent) with { Expires = now.AddMinutes(-1) },
            Seed("echo", "b", CommitmentKind.Declined, CommitmentOrigin.Agent) with
            {
                Scope = CommitmentScope.Conversation, ConversationId = "other"
            },
            Seed("echo", "c", CommitmentKind.Declined, CommitmentOrigin.Agent) with
            {
                Scope = CommitmentScope.User, UserId = "local"
            }
        };

        var live = CommitmentLedger.Live(all, now, "this", "local");

        Assert.Equal("c", Assert.Single(live).Subject);
    }

    private static ITransitionStore Seeded(params Transition[] history)
    {
        var store = new InMemoryTransitionStore();

        foreach (var transition in history)
        {
            store.Append(transition.Clone());
        }

        return store;
    }

    private static Commitment Seed(
        string capability,
        string subject,
        CommitmentKind kind,
        CommitmentOrigin origin = CommitmentOrigin.Reviewer)
    {
        return new Commitment(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "privacy",
            CommitmentScope.Deployment,
            $"Seeded: {capability} {subject}",
            origin,
            DateTimeOffset.UtcNow,
            null)
        {
            Kind = kind,
            Capability = capability,
            Subject = PlanSimilarity.Normalise(subject)
        };
    }
}

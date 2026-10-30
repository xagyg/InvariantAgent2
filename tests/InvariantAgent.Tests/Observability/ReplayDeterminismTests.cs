using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Drift;
using InvariantAgent.Core.Extensions;
using InvariantAgent.Core.Model.Agent;
using InvariantAgent.Core.Control;
using InvariantAgent.Core.Model.Drift;
using InvariantAgent.Core.Model.Transition;
using InvariantAgent.Core.Replay;
using InvariantAgent.Safety.Invariants.Action;
using InvariantAgent.Safety.Invariants.Identity;
using Xunit;

namespace InvariantAgent.Tests.Observability;

/// <summary>
/// Replay must depend only on the recorded transition and the current rules: not on baselines approved
/// after the transition ran, and without side effects on drift statistics.
/// </summary>
public sealed class ReplayDeterminismTests
{
    // A baseline far enough from the transition's state to exceed the drift bound (score 100 vs bound 70).
    private static AgentState DistantBaselineState() => new()
    {
        Goal = "a different objective",
        Mode = "strict",
        Policies = { "p1", "p2", "p3" },
        Memory = { ["notes"] = "unrelated" }
    };

    // Completed transition whose After state is close to Before (score 40, under the bound).
    private static Transition CompletedTransition(DriftBaselineSnapshot recordedBaseline) => new()
    {
        Input = "memory-set goal=ship",
        Before = new AgentState(),
        After = new AgentState { Version = 1, Memory = { ["goal"] = "ship" } },
        ProposedAction = new AgentAction { Capability = "memory-set", Input = "goal=ship" },
        Phase = TransitionPhase.Completed,
        Status = TransitionStatus.Completed,
        DriftBaseline = recordedBaseline
    };

    private static (ReplayValidator Validator, IDriftBaselineStore Baselines, DriftTracker Tracker) CreateValidator()
    {
        var baselines = new InMemoryDriftBaselineStore();
        var tracker = new DriftTracker();
        var evaluator = new InvariantEvaluator(new IInvariant[]
        {
            new BehaviouralDriftBoundInvariant(new BehaviouralDriftDetector(baselines))
        });

        return (new ReplayValidator(evaluator, tracker), baselines, tracker);
    }

    [Fact]
    public void Replay_UsesRecordedBaseline_NotOneApprovedLater()
    {
        var (validator, baselines, _) = CreateValidator();

        // Ran with no approved baseline; an operator approves a very different one afterwards.
        var transition = CompletedTransition(DriftBaselineSnapshot.From(null));
        baselines.Approve(DistantBaselineState(), "approved after the transition ran");

        var result = validator.Validate(new TransitionContext { Transition = transition.Clone() });

        Assert.True(result.Passed);
        Assert.Equal(TransitionStatus.Completed, result.Comparison.ReplayStatus);
        Assert.True(result.Comparison.DriftBaselineRecorded);
    }

    [Fact]
    public void Replay_UsesRecordedApprovedBaseline_AfterBaselineIsReplaced()
    {
        var (validator, baselines, _) = CreateValidator();

        // Ran under a baseline matching its own state; later replaced by a distant one.
        var original = baselines.Approve(new AgentState { Memory = { ["goal"] = "ship" } }, "original");
        var transition = CompletedTransition(DriftBaselineSnapshot.From(original));
        baselines.Approve(DistantBaselineState(), "replacement");

        var result = validator.Validate(new TransitionContext { Transition = transition.Clone() });

        Assert.True(result.Passed);
        Assert.Equal(0, result.Score);
    }

    [Fact]
    public void Replay_WithoutRecordedBaseline_FallsBackToCurrentAndSaysSo()
    {
        var (validator, baselines, _) = CreateValidator();

        // Legacy history: no baseline captured, so replay can only use the current one.
        var transition = CompletedTransition(null);
        baselines.Approve(DistantBaselineState(), "approved after the transition ran");

        var result = validator.Validate(new TransitionContext { Transition = transition.Clone() });

        Assert.False(result.Passed);
        Assert.False(result.Comparison.DriftBaselineRecorded);
    }

    [Fact]
    public void Replay_IsReportOnlyByDefault()
    {
        var tracker = new DriftTracker();
        var validator = new ReplayValidator(
            new InvariantEvaluator(new IInvariant[] { new NoDeleteInvariant() }),
            tracker);

        // Recorded as completed, but current rules reject it: a mismatch.
        var result = validator.Validate(new TransitionContext
        {
            Transition = new Transition
            {
                Input = "delete everything",
                Before = new AgentState(),
                Phase = TransitionPhase.Completed,
                Status = TransitionStatus.Completed,
                ProposedAction = new AgentAction { Capability = "delete", Input = "everything" }
            }
        });

        Assert.False(result.Passed);
        Assert.Empty(tracker.Records);
    }

    [Fact]
    public void RecordedBaseline_SurvivesClone()
    {
        var baseline = new InMemoryDriftBaselineStore()
            .Approve(new AgentState { Version = 3, Memory = { ["goal"] = "ship" } }, "reviewed");

        var clone = CompletedTransition(DriftBaselineSnapshot.From(baseline)).Clone();

        Assert.Equal(baseline.Id, clone.DriftBaseline.BaselineId);
        Assert.Equal(3, clone.DriftBaseline.StateVersion);
        Assert.Equal("ship", Assert.IsType<string>(clone.DriftBaseline.State.Memory["goal"]));
    }
}

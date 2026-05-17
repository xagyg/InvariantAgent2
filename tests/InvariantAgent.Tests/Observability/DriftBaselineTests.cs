using InvariantAgent.Core.Drift;
using InvariantAgent.Core.Model.Transition;
using Xunit;

namespace InvariantAgent.Tests.Observability;

public sealed class DriftBaselineTests
{
    [Fact]
    public async Task BaselineApprove_StoresCurrentCommittedState()
    {
        var fixture = TestFixture.Create();

        await fixture.Runtime.RunAsync("memory-set goal=stable baseline");
        var context = await fixture.Runtime.RunAsync("baseline-approve after review");

        var baseline = fixture.DriftBaselineStore.Current;

        Assert.Equal(TransitionStatus.Completed, context.Transition.Status);
        Assert.NotNull(baseline);
        Assert.Equal(1, baseline.StateVersion);
        Assert.Equal("stable baseline", baseline.State.Memory["goal"]);
        Assert.Equal("after review", baseline.Reason);
    }

    [Fact]
    public async Task BaselineShow_ReturnsApprovedBaselineDetails()
    {
        var fixture = TestFixture.Create();

        await fixture.Runtime.RunAsync("memory-set goal=visible baseline");
        await fixture.Runtime.RunAsync("baseline-approve reviewer accepted");
        var context = await fixture.Runtime.RunAsync("baseline-show");

        Assert.Equal(TransitionStatus.Completed, context.Transition.Status);
        Assert.Contains("==== DRIFT BASELINE ====", context.Transition.Outcome.Result);
        Assert.Contains("StateVersion: 1", context.Transition.Outcome.Result);
        Assert.Contains("Reason: reviewer accepted", context.Transition.Outcome.Result);
        Assert.Contains("goal=visible baseline", context.Transition.Outcome.Result);
    }

    [Fact]
    public async Task Detector_UsesApprovedBaselineForTransitionScoring()
    {
        var fixture = TestFixture.Create();

        await fixture.Runtime.RunAsync("memory-set goal=approved goal");
        await fixture.Runtime.RunAsync("baseline-approve approved goal");

        var transition = fixture.Store.GetAll()
            .First(t => t.Input == "memory-set goal=approved goal");

        var detector = new BehaviouralDriftDetector(fixture.DriftBaselineStore);
        var score = detector.Score(transition);

        Assert.Equal(0, score.Score);
    }
}

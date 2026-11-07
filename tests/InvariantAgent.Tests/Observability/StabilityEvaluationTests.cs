using InvariantAgent.Core.Model.Stability;
using Xunit;

namespace InvariantAgent.Tests.Observability;

/// <summary>
/// Tests for behavioural stability evaluation (AIE metrics): stable traces, the effect of goal changes on
/// continuity and persistence, invariant failures moving the region to Watch, and drift-tool output.
/// </summary>
public sealed class StabilityEvaluationTests
{
    [Fact]
    public async Task Analyze_WithStableOperationalTrace_ReturnsStableRegion()
    {
        var fixture = TestFixture.Create();

        await fixture.Runtime.RunAsync("echo hello");
        await fixture.Runtime.RunAsync("calc 10+20");
        await fixture.Runtime.RunAsync("echo hello");

        var report = fixture.DriftAnalyzer.Analyze(fixture.Store.GetAll());

        Assert.Equal(StabilityRegion.Stable, report.Stability.Region);
        Assert.True(report.Stability.Current.Continuity >= 90);
        Assert.True(report.Stability.Current.Persistence >= 90);
    }

    [Fact]
    public async Task Analyze_WithAdaptiveGoalChange_LowersContinuityAndPersistence()
    {
        var fixture = TestFixture.Create();

        await fixture.Runtime.RunAsync("memory-set goal=initial goal");
        await fixture.Runtime.RunAsync("memory-set goal=changed goal");

        var report = fixture.DriftAnalyzer.Analyze(fixture.Store.GetAll());

        Assert.True(report.Stability.Current.Continuity < 100);
        Assert.True(report.Stability.Current.Persistence < 100);
        Assert.Contains(
            report.Stability.Recommendations,
            recommendation => recommendation.Contains("monitoring", StringComparison.OrdinalIgnoreCase) ||
                              recommendation.Contains("validation", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Analyze_WithInvariantFailure_EntersWatchRegion()
    {
        var fixture = TestFixture.Create();

        await fixture.Runtime.RunAsync("echo hello");
        await fixture.Runtime.RunAsync("does-not-exist thing");

        var report = fixture.DriftAnalyzer.Analyze(fixture.Store.GetAll());

        Assert.Equal(StabilityRegion.Watch, report.Stability.Region);
        Assert.Contains(
            report.Stability.Recommendations,
            recommendation => recommendation.Contains("review", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Identifiability_CountsRejectionsInTheCurrentWindow()
    {
        // The window holds only completed transitions; identifiability must still see rejections in its span.
        var fixture = TestFixture.Create();

        await fixture.Runtime.RunAsync("echo hello");
        await fixture.Runtime.RunAsync("does-not-exist thing");
        await fixture.Runtime.RunAsync("echo hello");

        var report = fixture.DriftAnalyzer.Analyze(fixture.Store.GetAll());

        Assert.Equal(85, report.Stability.Current.Identifiability);
    }

    [Fact]
    public async Task Consistency_IsMeasuredToTheEndOfEachWindow_SoItContributesToDrift()
    {
        var fixture = TestFixture.Create();

        // Previous window: no repeated action yet, so consistency is 100.
        await fixture.Runtime.RunAsync("memory-show");
        await fixture.Runtime.RunAsync("memory-set notes=first");
        await fixture.Runtime.RunAsync("echo hello");

        // Current window: memory-show repeats with a different result.
        await fixture.Runtime.RunAsync("memory-show");
        await fixture.Runtime.RunAsync("echo hello");
        await fixture.Runtime.RunAsync("echo hello");

        var stability = fixture.DriftAnalyzer.Analyze(fixture.Store.GetAll()).Stability;

        Assert.NotNull(stability.Previous);
        Assert.Equal(100, stability.Previous!.Consistency);
        Assert.True(stability.Current.Consistency < 100);
        Assert.True(stability.DriftMagnitude >= 100 - stability.Current.Consistency);
    }

    [Fact]
    public async Task Recovery_IsMeasuredToTheEndOfEachWindow()
    {
        var fixture = TestFixture.Create();

        // Previous window: clean.
        await fixture.Runtime.RunAsync("echo one");
        await fixture.Runtime.RunAsync("echo two");
        await fixture.Runtime.RunAsync("echo three");

        // Current window, ending in a rejection the agent has not yet recovered from.
        await fixture.Runtime.RunAsync("echo four");
        await fixture.Runtime.RunAsync("echo five");
        await fixture.Runtime.RunAsync("echo six");
        await fixture.Runtime.RunAsync("does-not-exist thing");

        var stability = fixture.DriftAnalyzer.Analyze(fixture.Store.GetAll()).Stability;

        // Scored over the whole history, both windows read 0 and recovery never moved drift magnitude.
        Assert.Equal(100, stability.Previous!.Recovery);
        Assert.Equal(0, stability.Current.Recovery);
        Assert.True(stability.DriftMagnitude >= 100);
    }

    [Fact]
    public async Task DriftTool_ShowsStabilityEvaluation()
    {
        var fixture = TestFixture.Create();

        await fixture.Runtime.RunAsync("echo hello");
        var context = await fixture.Runtime.RunAsync("drift");

        Assert.Contains("Stability region:", context.Transition.Outcome.Result);
        Assert.Contains("Stability vector:", context.Transition.Outcome.Result);
        Assert.Contains("Governance recommendations:", context.Transition.Outcome.Result);
    }
}

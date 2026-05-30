using InvariantAgent.Core.Model.Stability;
using Xunit;

namespace InvariantAgent.Tests.Observability;

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

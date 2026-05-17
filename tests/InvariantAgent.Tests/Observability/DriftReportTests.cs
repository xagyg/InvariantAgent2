using InvariantAgent.Core.Model.Control;
using InvariantAgent.Core.Model.Drift;
using InvariantAgent.Core.Model.Transition;
using Xunit;

namespace InvariantAgent.Tests.Observability;

public sealed class DriftReportTests
{
    [Fact]
    public void Analyze_WithRecordedDrift_ReturnsDriftCounts()
    {
        var fixture = TestFixture.Create();

        fixture.DriftTracker.Record(
            new DriftRecord
            {
                Type = DriftType.GovernanceDrift,
                Reason = "Replay validation mismatch.",
                TransitionId = "test-transition",
                TimestampUtc = DateTime.UtcNow,
                Phase = TransitionPhase.ExecutionValidation,
                Severity = InvariantSeverity.Critical
            });

        var report = fixture.DriftAnalyzer.Analyze(fixture.Store.GetAll());

        Assert.True(report.DriftCounts.ContainsKey(DriftType.GovernanceDrift));
        Assert.Equal(1, report.DriftCounts[DriftType.GovernanceDrift]);
        Assert.Single(report.RecentDrift);
    }

    [Fact]
    public async Task Analyze_WithGoalChange_ReturnsBehaviouralDrift()
    {
        var fixture = TestFixture.Create();

        await fixture.Runtime.RunAsync("memory-set goal=ship behavioural drift");

        var report = fixture.DriftAnalyzer.Analyze(fixture.Store.GetAll());

        Assert.True(report.DriftCounts.ContainsKey(DriftType.BehaviouralDrift));
        Assert.Equal(1, report.DriftCounts[DriftType.BehaviouralDrift]);
        Assert.True(report.TotalDriftScore > 0);
        Assert.Contains(
            report.RecentDrift,
            drift => drift.Type == DriftType.BehaviouralDrift &&
                     drift.Reason.Contains("goal changed"));
    }

    [Fact]
    public async Task Analyze_WithOnlyVolatileOutcomeMemory_DoesNotReturnBehaviouralDrift()
    {
        var fixture = TestFixture.Create();

        await fixture.Runtime.RunAsync("echo hello");
        await fixture.Runtime.RunAsync("calc 10+20");

        var report = fixture.DriftAnalyzer.Analyze(fixture.Store.GetAll());

        Assert.False(report.DriftCounts.ContainsKey(DriftType.BehaviouralDrift));
    }
}

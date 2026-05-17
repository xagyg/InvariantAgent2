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

    [Fact]
    public async Task Analyze_WithInvariantFailures_GroupsFailuresByCategory()
    {
        var fixture = TestFixture.Create();

        await fixture.Runtime.RunAsync("does-not-exist thing");
        await fixture.Runtime.RunAsync("memory-set password=secret");

        var report = fixture.DriftAnalyzer.Analyze(fixture.Store.GetAll());

        Assert.Equal(1, report.InvariantFailuresByCategory[InvariantCategory.Safety]);
        Assert.Equal(1, report.InvariantFailuresByCategory[InvariantCategory.SelfModification]);
        Assert.Equal(1, report.InvariantFailures["AllowedCapabilityInvariant [Safety]"]);
        Assert.Equal(1, report.InvariantFailures["AllowedMemoryKeyInvariant [SelfModification]"]);
    }

    [Fact]
    public async Task DriftTool_WithInvariantFailures_ShowsCategoryGrouping()
    {
        var fixture = TestFixture.Create();

        await fixture.Runtime.RunAsync("does-not-exist thing");
        await fixture.Runtime.RunAsync("memory-set password=secret");
        var context = await fixture.Runtime.RunAsync("drift");

        Assert.Contains("Invariant failures by category:", context.Transition.Outcome.Result);
        Assert.Contains("Safety: 1", context.Transition.Outcome.Result);
        Assert.Contains("SelfModification: 1", context.Transition.Outcome.Result);
    }
}

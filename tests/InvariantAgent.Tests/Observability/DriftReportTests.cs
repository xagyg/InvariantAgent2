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

        var report = fixture.DriftAnalyzer.Analyze(
            fixture.Store.GetAll());

        Assert.True(report.DriftCounts.ContainsKey(DriftType.GovernanceDrift));
        Assert.Equal(1, report.DriftCounts[DriftType.GovernanceDrift]);
        Assert.Single(report.RecentDrift);
    }
}
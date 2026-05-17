using InvariantAgent.Core.Rendering;
using Xunit;

namespace InvariantAgent.Tests.Observability;

public sealed class ExplainReportTests
{
    [Fact]
    public async Task FormatExplain_IncludesReductionInvariants()
    {
        var fixture = TestFixture.Create();

        var context = await fixture.Runtime.RunAsync("memory-set goal=show reduction governance");

        var report = TransitionFormatter.FormatExplain(context.Transition);

        Assert.Contains("Reduction invariants:", report);
        Assert.Contains("BehaviouralDriftBoundInvariant", report);
    }
}


using InvariantAgent.Core.Model.Transition;
using Xunit;

namespace InvariantAgent.Tests.Observability;

public sealed class ReplayTests
{
    [Fact]
    public async Task RunAsync_WithReplayCommand_ReturnsTransitionHistory()
    {
        var fixture = TestFixture.Create();

        await fixture.Runtime.RunAsync("echo first");

        var context = await fixture.Runtime.RunAsync("replay");

        Assert.Equal(TransitionStatus.Completed, context.Transition.Status);

        var result = context.Transition.Outcome?.Result ?? "";

        Assert.Contains("==== REPLAY START ====", result);
        Assert.Contains("echo", result);
        Assert.Contains("Completed", result);
    }

    [Fact]
    public async Task RunAsync_WithReplayCount_ReturnsOnlyRequestedNumberOfTransitions()
    {
        var fixture = TestFixture.Create();

        await fixture.Runtime.RunAsync("echo one");
        await fixture.Runtime.RunAsync("echo two");

        var context = await fixture.Runtime.RunAsync("replay 1");

        var result = context.Transition.Outcome?.Result ?? "";

        Assert.DoesNotContain("echo one", result);
        Assert.Contains("echo two", result);
    }
}

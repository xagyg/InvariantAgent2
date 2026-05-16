
using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Transition;
using InvariantAgent.Hosting;
using InvariantAgent.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace InvariantAgent.Tests.Runtime;

public sealed class RuntimeExecutionTests
{
    [Fact]
    public async Task RunAsync_WithEchoCommand_CompletesAndStoresTransition()
    {
        var fixture = TestFixture.Create();

        var context = await fixture.Runtime.RunAsync("echo hello world");

        Assert.Equal(TransitionStatus.Completed, context.Transition.Status);
        Assert.True(context.Transition.Outcome?.Success);
        Assert.Equal("echo", context.Transition.ProposedAction?.Capability);
        Assert.Equal("hello world", context.Transition.Outcome?.Result);
        Assert.Equal(1, fixture.Runtime.State.Version);
        Assert.Single(fixture.Store.GetAll());
    }

    [Fact]
    public async Task RunAsync_WithCalcCommand_CompletesAndStoresResult()
    {
        var fixture = TestFixture.Create();

        var context = await fixture.Runtime.RunAsync("calc 10+20");

        Assert.Equal(TransitionStatus.Completed, context.Transition.Status);
        Assert.True(context.Transition.Outcome?.Success);
        Assert.Equal("30", context.Transition.Outcome?.Result);
    }

    [Fact]
    public async Task RunAsync_WithSearchCommand_CompletesAndReturnsResults()
    {
        var fixture = TestFixture.Create();

        var context = await fixture.Runtime.RunAsync("search redis");

        Assert.Equal(TransitionStatus.Completed, context.Transition.Status);
        Assert.Contains("Result 1 for 'redis'", context.Transition.Outcome?.Result);
    }
}

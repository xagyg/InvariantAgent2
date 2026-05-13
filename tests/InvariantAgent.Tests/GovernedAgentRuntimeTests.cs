using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Transition;
using InvariantAgent.Hosting;
using InvariantAgent.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace InvariantAgent.Tests;

public sealed class GovernedAgentRuntimeTests
{
    [Fact]
    public async Task RunAsync_WithEchoCommand_CompletesAndStoresTransition()
    {
        var fixture = CreateFixture();

        var context = await fixture.Runtime.RunAsync("echo hello world");

        Assert.Equal(TransitionStatus.Completed, context.Transition.Status);
        Assert.True(context.Transition.Outcome?.Success);
        Assert.Equal("echo", context.Transition.ProposedAction?.Capability);
        Assert.Equal("hello world", context.Transition.Outcome?.Result);
        Assert.Equal(1, fixture.Runtime.State.Version);
        Assert.Single(fixture.Store.GetAll());
        Assert.Same(context.Transition, fixture.Store.GetAll()[0]);
    }

    [Fact]
    public async Task RunAsync_WithUnknownCapability_IsRejectedBeforeExecution()
    {
        var fixture = CreateFixture();

        var context = await fixture.Runtime.RunAsync("does-not-exist something");

        Assert.Equal(TransitionStatus.Rejected, context.Transition.Status);
        Assert.Contains("not allowed", context.Transition.Reason, StringComparison.OrdinalIgnoreCase);
        Assert.Null(context.Transition.Outcome);
        Assert.Null(context.Transition.After);
        Assert.Equal(0, fixture.Runtime.State.Version);
        Assert.Single(fixture.Store.GetAll());
    }

    [Fact]
    public async Task RunAsync_WithAllowedMemorySet_AppliesMemoryChange()
    {
        var fixture = CreateFixture();

        var context = await fixture.Runtime.RunAsync("memory-set goal=ship tests");

        Assert.Equal(TransitionStatus.Completed, context.Transition.Status);
        Assert.NotNull(context.Transition.SelfModification);
        Assert.Equal("goal", context.Transition.SelfModification!.Key);
        Assert.Equal("ship tests", fixture.Runtime.State.Memory["goal"]);
        Assert.Equal("ship tests", context.Transition.After?.Memory["goal"]);
    }

    [Fact]
    public async Task RunAsync_WithDisallowedMemorySet_IsRejectedAndDoesNotCommitState()
    {
        var fixture = CreateFixture();

        var context = await fixture.Runtime.RunAsync("memory-set password=secret");

        Assert.Equal(TransitionStatus.Rejected, context.Transition.Status);
        Assert.Contains("Memory key 'password' is not allowed", context.Transition.Reason);
        Assert.Null(context.Transition.After);
        Assert.False(fixture.Runtime.State.Memory.ContainsKey("password"));
        Assert.Equal(0, fixture.Runtime.State.Version);
        Assert.Single(fixture.Store.GetAll());
    }

    [Fact]
    public async Task RunAsync_WithReplayCommand_ReturnsTransitionHistory()
    {
        var fixture = CreateFixture();

        await fixture.Runtime.RunAsync("echo first");
        var context = await fixture.Runtime.RunAsync("replay");

        Assert.Equal(TransitionStatus.Completed, context.Transition.Status);
        Assert.Contains("==== REPLAY START ====", context.Transition.Outcome?.Result);
        Assert.Contains("Capability=echo", context.Transition.Outcome?.Result);
        Assert.Contains("Status=Completed", context.Transition.Outcome?.Result);
        Assert.Equal(2, fixture.Store.GetAll().Count);
    }

    [Fact]
    public async Task RunAsync_WithCalcCommand_CompletesAndStoresResult()
    {
        var fixture = CreateFixture();

        var context = await fixture.Runtime.RunAsync("calc 10+20");

        Assert.Equal(TransitionStatus.Completed, context.Transition.Status);
        Assert.True(context.Transition.Outcome?.Success);
        Assert.Equal("30", context.Transition.Outcome?.Result);
        Assert.Equal("30", fixture.Runtime.State.Memory["lastOutcome"]);
    }

    [Fact]
    public async Task RunAsync_WithSearchCommand_CompletesAndReturnsResults()
    {
        var fixture = CreateFixture();

        var context = await fixture.Runtime.RunAsync("search redis");

        Assert.Equal(TransitionStatus.Completed, context.Transition.Status);
        Assert.Contains("Result 1 for 'redis'", context.Transition.Outcome?.Result);
        Assert.Contains("Result 2 for 'redis'", context.Transition.Outcome?.Result);
    }

    [Fact]
    public async Task RunAsync_WithMemoryShowBeforeMemoryExists_ReturnsMemoryEmpty()
    {
        var fixture = CreateFixture();

        var context = await fixture.Runtime.RunAsync("memory-show");

        Assert.Equal(TransitionStatus.Completed, context.Transition.Status);
        Assert.Contains("Memory is empty", context.Transition.Outcome?.Result);
    }

    [Fact]
    public async Task RunAsync_WithMemoryShowAfterMemorySet_ReturnsMemoryContents()
    {
        var fixture = CreateFixture();

        await fixture.Runtime.RunAsync("memory-set goal=ship tests");
        var context = await fixture.Runtime.RunAsync("memory-show");

        Assert.Equal(TransitionStatus.Completed, context.Transition.Status);
        Assert.Contains("goal=ship tests", context.Transition.Outcome?.Result);
    }

    [Fact]
    public async Task RunAsync_WithReplayCount_ReturnsOnlyRequestedNumberOfTransitions()
    {
        var fixture = CreateFixture();

        await fixture.Runtime.RunAsync("echo one");
        await fixture.Runtime.RunAsync("echo two");
        var context = await fixture.Runtime.RunAsync("replay 1");

        Assert.Equal(TransitionStatus.Completed, context.Transition.Status);
        Assert.DoesNotContain("Input=echo one", context.Transition.Outcome?.Result);
        Assert.Contains("Input=echo two", context.Transition.Outcome?.Result);
    }

    [Fact]
    public async Task RunAsync_WithDeleteCommand_IsRejectedByInvariant()
    {
        var fixture = CreateFixture();

        var context = await fixture.Runtime.RunAsync("delete everything");

        Assert.Equal(TransitionStatus.Rejected, context.Transition.Status);
        Assert.Contains("Delete operations are not allowed", context.Transition.Reason);
        Assert.Null(context.Transition.Outcome);
        Assert.Equal(0, fixture.Runtime.State.Version);
    }

    [Fact]
    public async Task RunAsync_WithPlannerError_IsRejectedBeforeExecution()
    {
        var fixture = CreateFixture();

        var context = await fixture.Runtime.RunAsync("");

        Assert.Equal(TransitionStatus.Rejected, context.Transition.Status);
        Assert.Equal("planner", context.Transition.ProposedAction?.Capability);
        Assert.Contains("not allowed", context.Transition.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RunAsync_RejectedTransition_IsStoredButDoesNotAdvanceState()
    {
        var fixture = CreateFixture();

        await fixture.Runtime.RunAsync("echo ok");
        var rejected = await fixture.Runtime.RunAsync("memory-set password=secret");

        Assert.Equal(TransitionStatus.Rejected, rejected.Transition.Status);
        Assert.Equal(1, fixture.Runtime.State.Version);
        Assert.Equal(2, fixture.Store.GetAll().Count);
        Assert.Null(rejected.Transition.After);
    }

    [Fact]
public void AddInvariantAgent_RegistersSinglePreAndPostControl()
{
    var services = new ServiceCollection();
    services.AddInvariantAgent();

    var preControls = services.Where(s => s.ServiceType == typeof(IPreControl)).ToList();
    var postControls = services.Where(s => s.ServiceType == typeof(IPostControl)).ToList();

    Assert.Single(preControls);
    Assert.Single(postControls);
}

    private static RuntimeFixture CreateFixture()
    {
        var services = new ServiceCollection();
        services.AddInvariantAgent();

        var provider = services.BuildServiceProvider(validateScopes: true);

        return new RuntimeFixture(
            provider.GetRequiredService<GovernedAgentRuntime>(),
            provider.GetRequiredService<ITransitionStore>());
    }

    private sealed record RuntimeFixture(
        GovernedAgentRuntime Runtime,
        ITransitionStore Store);
}

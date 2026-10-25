
using InvariantAgent.Core.Model.Transition;
using Xunit;

namespace InvariantAgent.Tests.Governance;

/// <summary>
/// End-to-end tests for governed self-modification: allowed memory keys are assimilated, disallowed
/// keys are rejected without changing state.
/// </summary>
public sealed class SelfModificationGovernanceTests
{
    [Fact]
    public async Task RunAsync_WithAllowedMemorySet_AppliesMemoryChange()
    {
        var fixture = TestFixture.Create();

        var context = await fixture.Runtime.RunAsync("memory-set goal=ship tests");

        Assert.Equal(TransitionStatus.Completed, context.Transition.Status);
        Assert.Equal("ship tests", fixture.Runtime.State.Memory["goal"]);
    }

    [Fact]
    public async Task RunAsync_WithDisallowedMemorySet_IsRejectedAndDoesNotCommitState()
    {
        var fixture = TestFixture.Create();

        var context = await fixture.Runtime.RunAsync("memory-set password=secret");

        Assert.Equal(TransitionStatus.Rejected, context.Transition.Status);
        Assert.False(fixture.Runtime.State.Memory.ContainsKey("password"));
        Assert.Null(context.Transition.After);
    }
}

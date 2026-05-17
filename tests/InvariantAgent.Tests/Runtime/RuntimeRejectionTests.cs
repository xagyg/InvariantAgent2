
using InvariantAgent.Core.Model.Transition;
using Xunit;

namespace InvariantAgent.Tests.Runtime;

public sealed class RuntimeRejectionTests
{
    [Fact]
    public async Task RunAsync_WithUnknownCapability_IsRejectedBeforeExecution()
    {
        var fixture = TestFixture.Create();

        var context = await fixture.Runtime.RunAsync("does-not-exist something");

        Assert.Equal(TransitionStatus.Rejected, context.Transition.Status);
        Assert.Contains("not allowed", context.Transition.Reason, System.StringComparison.OrdinalIgnoreCase);
        Assert.Null(context.Transition.Outcome);
        Assert.Null(context.Transition.After);
    }

    [Fact]
    public async Task RunAsync_WithDeleteCommand_IsRejectedByInvariant()
    {
        var fixture = TestFixture.Create();

        var context = await fixture.Runtime.RunAsync("delete everything");

        Assert.Equal(TransitionStatus.Rejected, context.Transition.Status);
        Assert.Contains("Delete operations are not allowed", context.Transition.Reason);
    }

    [Fact]
    public async Task RunAsync_RejectedTransition_IsStoredButDoesNotAdvanceState()
    {
        var fixture = TestFixture.Create();

        await fixture.Runtime.RunAsync("echo ok");
        var rejected = await fixture.Runtime.RunAsync("memory-set password=secret");

        Assert.Equal(TransitionStatus.Rejected, rejected.Transition.Status);
        Assert.Equal(1, fixture.Runtime.State.Version);
        Assert.Equal(2, fixture.Store.GetAll().Count);
        Assert.Null(rejected.Transition.After);
    }

    [Fact]
    public async Task RunAsync_WhenRejected_DoesNotRecordCommittedVersion()
    {
        var fixture = TestFixture.Create();

        var context = await fixture.Runtime.RunAsync("memory-set password=secret");

        Assert.Equal(TransitionStatus.Rejected, context.Transition.Status);
        Assert.DoesNotContain(
            context.Transition.Events,
            e => e.Metadata.TryGetValue("Committed", out var committed) &&
                 committed is true);
    }
}

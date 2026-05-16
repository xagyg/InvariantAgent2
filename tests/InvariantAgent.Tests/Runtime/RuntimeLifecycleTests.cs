
using InvariantAgent.Core.Model.Transition;
using Xunit;

namespace InvariantAgent.Tests.Runtime;

public sealed class RuntimeLifecycleTests
{
    [Fact]
    public async Task RunAsync_AllowedCommand_RecordsLifecyclePhases()
    {
        var fixture = TestFixture.Create();

        var context = await fixture.Runtime.RunAsync("echo hello");

        Assert.Equal(TransitionStatus.Completed, context.Transition.Status);
        Assert.Equal(TransitionPhase.Completed, context.Transition.Phase);

        Assert.Contains(context.Transition.Events, e =>
            e.Stage == TransitionEventStage.Lifecycle &&
            e.Message.Contains("Planning"));

        Assert.Contains(context.Transition.Events, e =>
            e.Stage == TransitionEventStage.Lifecycle &&
            e.Message.Contains("Execution"));

        Assert.Contains(context.Transition.Events, e =>
            e.Stage == TransitionEventStage.Lifecycle &&
            e.Message.Contains("Completed"));
    }
}

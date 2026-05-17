
using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Drift;
using InvariantAgent.Core.Model.Agent;
using InvariantAgent.Core.Model.Transition;
using InvariantAgent.Hosting;
using InvariantAgent.Runtime;
using Microsoft.Extensions.DependencyInjection;
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

    [Fact]
    public async Task RunAsync_WhenBehaviouralDriftExceedsBound_IsRejectedBeforeCommit()
    {
        var fixture = CreateFixtureWithReducer<HighDriftReducer>();

        var context = await fixture.Runtime.RunAsync("echo proposed jump");

        Assert.Equal(TransitionStatus.Rejected, context.Transition.Status);
        Assert.Contains("BehaviouralDriftBoundInvariant", context.Transition.Reason);
        Assert.Contains("Behavioural drift score", context.Transition.Reason);
        Assert.Equal(0, fixture.Runtime.State.Version);
        Assert.NotNull(context.Transition.After);
        Assert.DoesNotContain(
            context.Transition.Events,
            e => e.Metadata.TryGetValue("Committed", out var committed) &&
                 committed is true);
    }

    private static TestFixture CreateFixtureWithReducer<TReducer>()
        where TReducer : class, IStateReducer
    {
        var services = new ServiceCollection();

        services.AddInvariantAgent();
        services.AddSingleton<IStateReducer, TReducer>();

        var provider = services.BuildServiceProvider(validateScopes: true);

        return new TestFixture(
            provider.GetRequiredService<GovernedAgentRuntime>(),
            provider.GetRequiredService<ITransitionStore>(),
            provider.GetRequiredService<IDriftAnalyzer>(),
            provider.GetRequiredService<DriftTracker>(),
            provider.GetRequiredService<IDriftBaselineStore>());
    }

    private sealed class HighDriftReducer : IStateReducer
    {
        public void Apply(TransitionContext context)
        {
            context.Transition.After = new AgentState
            {
                Version = context.Transition.Before.Version + 1,
                Goal = "replace operating objective",
                Mode = "experimental",
                Policies =
                {
                    "prefer speed over auditability",
                    "use unreviewed tools"
                },
                Memory =
                {
                    ["goal"] = "replace operating objective",
                    ["notes"] = "skip review",
                    ["user_intent"] = "act autonomously"
                }
            };

            context.Transition.Status = TransitionStatus.Completed;
        }
    }
}

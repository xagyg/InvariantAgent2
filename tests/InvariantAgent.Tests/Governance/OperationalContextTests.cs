using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Control;
using InvariantAgent.Core.Model.Control;
using InvariantAgent.Core.Model.Transition;
using InvariantAgent.Hosting;
using InvariantAgent.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace InvariantAgent.Tests.Governance;

/// <summary>
/// HIG-C operational context (IBAAM 3 revised, Section 5) is wired into the runtime: stamped on every transition, changed only by
/// the operator, and recorded so replay resolves conflicts under the same context.
/// </summary>
public sealed class OperationalContextTests
{
    [Fact]
    public async Task Runtime_StampsTheConfiguredContextOnEachTransition()
    {
        var provider = Build(OperationalContext.Degraded);

        var context = await provider.GetRequiredService<GovernedAgentRuntime>().RunAsync("echo hello");

        Assert.Equal(OperationalContext.Degraded, context.Transition.OperationalContext);
        Assert.Equal(OperationalContext.Degraded, context.OperationalContext);
        Assert.Contains(context.Transition.Events, e => e.Message == "OperationalContext=Degraded");
    }

    [Fact]
    public async Task Runtime_DefaultsToNormal_AndAddsNoEvent()
    {
        var provider = Build(OperationalContext.Normal);

        var context = await provider.GetRequiredService<GovernedAgentRuntime>().RunAsync("echo hello");

        Assert.Equal(OperationalContext.Normal, context.Transition.OperationalContext);
        Assert.DoesNotContain(context.Transition.Events, e => e.Message.StartsWith("OperationalContext="));
    }

    [Fact]
    public async Task ContextCommand_AppliesFromTheNextTransition()
    {
        var provider = Build(OperationalContext.Normal);
        var runtime = provider.GetRequiredService<GovernedAgentRuntime>();

        var setting = await runtime.RunAsync("context emergency flood warning");
        var next = await runtime.RunAsync("echo status");

        Assert.Equal(TransitionStatus.Completed, setting.Transition.Status);
        Assert.Equal(OperationalContext.Normal, setting.Transition.OperationalContext);
        Assert.Equal(OperationalContext.Emergency, next.Transition.OperationalContext);
        Assert.Equal("flood warning", provider.GetRequiredService<OperationalContextState>().Reason);
    }

    [Fact]
    public async Task ContextCommand_RejectsUnknownContexts()
    {
        var provider = Build(OperationalContext.Normal);

        var context = await provider.GetRequiredService<GovernedAgentRuntime>().RunAsync("context panic");

        Assert.Equal(TransitionStatus.Rejected, context.Transition.Status);
        Assert.Equal(OperationalContext.Normal, provider.GetRequiredService<OperationalContextState>().Current);
    }

    [Fact]
    public void RecordedContext_GovernsReEvaluation()
    {
        // Replay wraps the recorded transition in a fresh context; it must see the recorded context, not Normal.
        var recorded = new Transition { OperationalContext = OperationalContext.Degraded };

        var replayContext = new TransitionContext { Transition = recorded };

        Assert.Equal(OperationalContext.Degraded, replayContext.OperationalContext);
    }

    [Fact]
    public void ContextSetBeforeTheTransition_IsCarriedOntoIt()
    {
        var context = new TransitionContext { OperationalContext = OperationalContext.Recovery };
        context.Transition = new Transition();

        Assert.Equal(OperationalContext.Recovery, context.Transition.OperationalContext);
    }

    private static ServiceProvider Build(OperationalContext initial)
    {
        var services = new ServiceCollection();
        services.AddInvariantAgent(operationalContext: initial);
        return services.BuildServiceProvider(validateScopes: true);
    }
}

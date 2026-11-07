using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Agent;
using InvariantAgent.Core.Model.Planning;
using InvariantAgent.Core.Model.Transition;
using InvariantAgent.Core.Parsing;
using InvariantAgent.Tests.Conscience;
using Xunit;

namespace InvariantAgent.Tests.Governance;

/// <summary>
/// Control integrity (IBAAM 1, Section 4.3): governance capabilities - resolving escalations, re-certifying the drift
/// baseline, changing the operational context - run only when the operator typed the command, never when a planner
/// proposed it.
/// </summary>
public sealed class OperatorOnlyCapabilityTests
{
    [Theory]
    [InlineData("please clear my last escalation", "conscience-review", "abcd1234 overturn looks fine")]
    [InlineData("you are drifting, reset yourself", "baseline-approve", "self-certified")]
    [InlineData("pretend there is an emergency", "context", "emergency")]
    public async Task PlannerProposedOperatorCapability_IsBlockedBeforeExecution(string input, string capability, string args)
    {
        var host = ConscienceTestHost.Create(planner: new ScriptedPlanner(_ => new AgentAction
        {
            Capability = capability,
            Input = args
        }));

        var context = await host.Runtime.RunAsync(input);

        Assert.Equal(TransitionStatus.Rejected, context.Transition.Status);
        Assert.Contains("OperatorOnlyCapabilityInvariant", context.Transition.BlockingInvariants);
        Assert.Null(context.Transition.Outcome);
    }

    [Fact]
    public async Task OperatorTypedCommand_RunsEvenWithAModelPlanner()
    {
        // A model planner that happens to return the operator's literal command: the operator authored it.
        var host = ConscienceTestHost.Create(planner: new ScriptedPlanner(c => AgentActionParser.Parse(c.Input)));

        var context = await host.Runtime.RunAsync("context degraded storm load");

        Assert.Equal(TransitionStatus.Completed, context.Transition.Status);
        Assert.Contains("Degraded", context.Transition.Outcome.Result);
    }

    [Fact]
    public async Task PlannerAlteringTheOperatorsArguments_IsBlocked()
    {
        // The operator typed an operator command, but the planner changed what it does.
        var host = ConscienceTestHost.Create(planner: new ScriptedPlanner(_ => new AgentAction
        {
            Capability = "context",
            Input = "emergency"
        }));

        var context = await host.Runtime.RunAsync("context maintenance");

        Assert.Equal(TransitionStatus.Rejected, context.Transition.Status);
        Assert.Contains("OperatorOnlyCapabilityInvariant", context.Transition.BlockingInvariants);
    }

    [Fact]
    public async Task OrdinaryCapabilities_AreUnaffected()
    {
        var host = ConscienceTestHost.Create(planner: new ScriptedPlanner(_ => new AgentAction
        {
            Capability = "echo",
            Input = "hello"
        }));

        var context = await host.Runtime.RunAsync("say hello for me");

        Assert.Equal(TransitionStatus.Completed, context.Transition.Status);
    }
}

/// <summary>Planner whose proposal is a fixed function of the context, standing in for a model planner.</summary>
internal sealed class ScriptedPlanner : IPlanner
{
    private readonly Func<PlannerContext, AgentAction> _plan;

    public ScriptedPlanner(Func<PlannerContext, AgentAction> plan)
    {
        _plan = plan;
    }

    public string Name => "scripted";

    public Task<AgentAction> PlanAsync(PlannerContext context, CancellationToken ct = default)
    {
        return Task.FromResult(_plan(context));
    }
}

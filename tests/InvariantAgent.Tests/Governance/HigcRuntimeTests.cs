using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Agent;
using InvariantAgent.Core.Model.Control;
using InvariantAgent.Core.Model.Planning;
using InvariantAgent.Core.Model.Transition;
using InvariantAgent.Core.Parsing;
using InvariantAgent.Core.Rendering;
using InvariantAgent.Tests.Conscience;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace InvariantAgent.Tests.Governance;

/// <summary>
/// End-to-end HIG-C behaviour through the production composition root: governed alternatives, conflict records,
/// explain output and drift attribution.
/// </summary>
public sealed class HigcRuntimeTests
{
    [Fact]
    public async Task RunAsync_WhenProposalIsBlockedButAlternativeIsClean_ExecutesTheAlternative()
    {
        // The proposal names a capability that does not exist (a fundamental violation); the planner's alternative is
        // clean, so it is the governed action and the one that runs.
        var host = ConscienceTestHost.Create(planner: new FallbackPlanner("echo"));

        var context = await host.Runtime.RunAsync("shout hello");

        Assert.Equal(TransitionStatus.Completed, context.Transition.Status);
        Assert.Equal("echo", context.Transition.ProposedAction?.Capability);
        Assert.Equal("hello", context.Transition.Outcome?.Result);
        Assert.Contains(context.Transition.Events, e => e.Message.StartsWith("Governed alternative selected"));

        var conflict = Assert.Single(context.Transition.Conflicts);
        Assert.Equal(InvariantScope.Plan, conflict.Scope);
        Assert.Equal("alternative-1", conflict.Selected);
        Assert.Equal("echo", conflict.SelectedCapability);
        Assert.True(conflict.ChangesBehaviour);

        var explain = TransitionFormatter.FormatExplain(context.Transition);
        Assert.Contains("Conflict resolution (HIG-C):", explain);

        var report = host.Provider.GetRequiredService<IDriftAnalyzer>().Analyze(host.Store.GetAll());
        Assert.Equal(1, report.ConflictTransitions);
        Assert.Empty(report.MissingExpectedChange);
    }

    [Fact]
    public async Task RunAsync_WhenEveryCandidateIsBlocked_DeclinesWithoutEscalation()
    {
        // Declining violates only the mission-level service obligation, so a fundamental violation never proceeds,
        // and since declining itself is fundamentally clean nothing is escalated.
        var host = ConscienceTestHost.Create(planner: new FallbackPlanner("whisper"));

        var context = await host.Runtime.RunAsync("shout hello");

        Assert.Equal(TransitionStatus.Rejected, context.Transition.Status);
        Assert.False(context.Transition.GovernanceEscalated);
        Assert.Contains("AllowedCapabilityInvariant", context.Transition.BlockingInvariants);
        Assert.Equal(GovernanceCandidate.DeclineId, Assert.Single(context.Transition.Conflicts).Selected);
    }

    [Fact]
    public async Task RunAsync_WithOrdinaryCommand_RecordsNoConflict()
    {
        var fixture = TestFixture.Create();

        var context = await fixture.Runtime.RunAsync("echo hello");

        Assert.Equal(TransitionStatus.Completed, context.Transition.Status);
        Assert.Empty(context.Transition.Conflicts);
        Assert.Contains(context.Transition.Events, e =>
            e.Stage == TransitionEventStage.Invariant &&
            e.Message.StartsWith("ServiceObligationInvariant: Passed"));
    }

    /// <summary>Command planner that offers one fixed fallback capability, with the same input, as an alternative.</summary>
    private sealed class FallbackPlanner : IAlternativePlanner
    {
        private readonly string _fallback;

        public FallbackPlanner(string fallback)
        {
            _fallback = fallback;
        }

        public string Name => "fallback";

        public Task<AgentAction> PlanAsync(PlannerContext context, CancellationToken ct = default) =>
            Task.FromResult(AgentActionParser.Parse(context.Input));

        public Task<IReadOnlyList<AgentAction>> ProposeAlternativesAsync(
            PlannerContext context,
            AgentAction proposed,
            CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<AgentAction>>(new[]
            {
                new AgentAction { Capability = _fallback, Input = proposed.Input }
            });
    }
}

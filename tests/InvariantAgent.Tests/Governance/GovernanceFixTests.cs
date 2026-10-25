using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Control;
using InvariantAgent.Core.Drift;
using InvariantAgent.Core.Model.Agent;
using InvariantAgent.Core.Model.Transition;
using InvariantAgent.Core.Pipeline;
using InvariantAgent.Core.Replay;
using InvariantAgent.Hosting;
using InvariantAgent.Runtime;
using InvariantAgent.Safety.Invariants.Action;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace InvariantAgent.Tests.Governance;

/// <summary>
/// Regression tests for the fixes made after the October 2026 commenting pass: committed-state
/// lookups, capability exception handling, reducer state carry-over, case-insensitive capability
/// allow-listing, replay scores and the renamed non-empty input invariant.
/// </summary>
public sealed class GovernanceFixTests
{
    // ---- Fix 1: rejected states are never treated as committed ----

    [Fact]
    public async Task BaselineApprove_AfterReductionRejection_DoesNotApproveRejectedState()
    {
        var fixture = CreateFixtureWithReducer<HighDriftReducer>();

        var rejected = await fixture.Runtime.RunAsync("echo proposed jump");
        var approve = await fixture.Runtime.RunAsync("baseline-approve accept jump");

        Assert.Equal(TransitionStatus.Rejected, rejected.Transition.Status);
        Assert.NotNull(rejected.Transition.After);
        Assert.Equal(TransitionStatus.Rejected, approve.Transition.Status);
        Assert.Contains("No committed state", approve.Transition.Reason);
        Assert.Null(fixture.DriftBaselineStore.Current);
    }

    [Fact]
    public async Task MemoryShow_AfterReductionRejection_IgnoresUncommittedState()
    {
        var fixture = CreateFixtureWithReducer<HighDriftReducer>();

        await fixture.Runtime.RunAsync("echo proposed jump");
        var context = await fixture.Runtime.RunAsync("memory-show");

        Assert.Equal("Memory is empty.", context.Transition.Outcome!.Result);
    }

    [Fact]
    public async Task RunAsync_EvaluatesRejectedTransitionCannotCommitInvariantAtReduction()
    {
        var fixture = TestFixture.Create();

        var context = await fixture.Runtime.RunAsync("echo hello");

        Assert.Equal(TransitionStatus.Completed, context.Transition.Status);
        Assert.Contains(
            context.Transition.Events,
            e => e.Stage == TransitionEventStage.Invariant &&
                 e.Message.StartsWith("RejectedTransitionCannotCommitInvariant: Passed"));
    }

    // ---- Fix 2: capability exceptions are contained ----

    [Fact]
    public async Task RunAsync_WhenCapabilityThrows_RejectsAndPersistsTransition()
    {
        var fixture = TestFixture.Create();

        var context = await fixture.Runtime.RunAsync("calc )(");

        Assert.Equal(TransitionStatus.Rejected, context.Transition.Status);
        Assert.False(context.Transition.Outcome!.Success);
        Assert.Contains("Capability 'calc' failed", context.Transition.Reason);
        Assert.Single(fixture.Store.GetAll());
        Assert.Equal(0, fixture.Runtime.State.Version);
    }

    [Fact]
    public async Task RunAsync_AfterCapabilityThrows_RuntimeContinues()
    {
        var fixture = TestFixture.Create();

        await fixture.Runtime.RunAsync("calc )(");
        var context = await fixture.Runtime.RunAsync("echo still running");

        Assert.Equal(TransitionStatus.Completed, context.Transition.Status);
        Assert.Equal(1, fixture.Runtime.State.Version);
    }

    // ---- Fix 3: reducer carries Mode, Goal and Policies forward ----

    [Fact]
    public void StateReducer_CarriesModeGoalAndPoliciesForward()
    {
        var before = new AgentState
        {
            Version = 3,
            Mode = "careful",
            Goal = "stay stable",
            Policies = { "require review" }
        };

        var context = new TransitionContext
        {
            Transition = new Transition
            {
                Before = before,
                Phase = TransitionPhase.Reduction,
                Status = TransitionStatus.Executed,
                ProposedAction = new AgentAction { Capability = "echo", Input = "hi" },
                Outcome = new AgentOutcome { Success = true, Capability = "echo", Result = "hi" }
            }
        };

        new StateReducer().Apply(context);

        var after = context.Transition.After;

        Assert.Equal(4, after.Version);
        Assert.Equal("careful", after.Mode);
        Assert.Equal("stay stable", after.Goal);
        Assert.Equal(new[] { "require review" }, after.Policies);
        Assert.NotSame(before.Policies, after.Policies);
    }

    // ---- Fix 4: capability allow-list matches the registry's case-insensitivity ----

    [Theory]
    [InlineData("calc", true)]
    [InlineData("CALC", true)]
    [InlineData("Calc", true)]
    [InlineData("calculator-x", false)]
    public void AllowedCapabilityInvariant_IsCaseInsensitive(string capability, bool expected)
    {
        var invariant = new AllowedCapabilityInvariant(new[] { "calc" });

        var result = invariant.Evaluate(new TransitionContext
        {
            Transition = new Transition
            {
                ProposedAction = new AgentAction { Capability = capability, Input = "1+1" }
            }
        });

        Assert.Equal(expected, result.Passed);
    }

    // ---- Fix 5: replay results carry their mismatch score ----

    [Fact]
    public void ReplayValidator_OnMismatch_ReturnsAndRecordsScore()
    {
        var tracker = new DriftTracker();
        var validator = new ReplayValidator(
            new InvariantEvaluator(new IInvariant[] { new NoDeleteInvariant() }),
            tracker);

        // Recorded as completed, but re-evaluation rejects it: status +100, phase +50, one extra violation +10.
        var result = validator.Validate(new TransitionContext
        {
            Transition = new Transition
            {
                Input = "delete everything",
                Before = new AgentState(),
                Phase = TransitionPhase.Completed,
                Status = TransitionStatus.Completed,
                ProposedAction = new AgentAction { Capability = "delete", Input = "everything" }
            }
        });

        Assert.False(result.Passed);
        Assert.Equal(160, result.Score);
        Assert.Equal(160, Assert.Single(tracker.Records).Score);
    }

    [Fact]
    public void ReplayValidator_OnMatch_ReturnsZeroScore()
    {
        var validator = new ReplayValidator(
            new InvariantEvaluator(new IInvariant[] { new NoDeleteInvariant() }),
            new DriftTracker());

        var result = validator.Validate(new TransitionContext
        {
            Transition = new Transition
            {
                Input = "echo hi",
                Before = new AgentState(),
                Phase = TransitionPhase.Completed,
                Status = TransitionStatus.Completed,
                ProposedAction = new AgentAction { Capability = "echo", Input = "hi" }
            }
        });

        Assert.True(result.Passed);
        Assert.Equal(0, result.Score);
    }

    // ---- Fix 7: renamed invariant ----

    [Fact]
    public void NonEmptyInputInvariant_RejectsEmptyInput()
    {
        var invariant = new NonEmptyInputInvariant();

        var result = invariant.Evaluate(new TransitionContext
        {
            Transition = new Transition
            {
                ProposedAction = new AgentAction { Capability = "echo", Input = " " }
            }
        });

        Assert.Equal("NonEmptyInputInvariant", invariant.Name);
        Assert.False(result.Passed);
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

    /// <summary>
    /// Reducer stub that proposes a large adaptive change so every transition is rejected by the
    /// drift-bound invariant at the Reduction checkpoint, leaving an uncommitted <c>After</c> state.
    /// </summary>
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

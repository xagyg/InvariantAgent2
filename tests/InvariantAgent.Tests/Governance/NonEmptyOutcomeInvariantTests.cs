using InvariantAgent.Core.Model.Agent;
using InvariantAgent.Core.Model.Transition;
using InvariantAgent.Safety.Invariants.Outcome;
using Xunit;

namespace InvariantAgent.Tests.Governance;

/// <summary>
/// NonEmptyOutcomeInvariant only judges successful outcomes, so a failed command is reported once
/// (by SuccessOutcomeInvariant) rather than twice.
/// </summary>
public sealed class NonEmptyOutcomeInvariantTests
{
    private static TransitionContext WithOutcome(AgentOutcome outcome) => new()
    {
        Transition = new Transition { Before = new AgentState(), Outcome = outcome }
    };

    [Fact]
    public void Evaluate_FailedOutcome_IsNotApplicable()
    {
        var result = new NonEmptyOutcomeInvariant().Evaluate(
            WithOutcome(new AgentOutcome { Success = false, Capability = "calc", Error = "Syntax error" }));

        Assert.True(result.Passed);
        Assert.Contains("Not applicable", result.Reason);
    }

    [Fact]
    public void Evaluate_SuccessfulOutcomeWithEmptyResult_Rejects()
    {
        var result = new NonEmptyOutcomeInvariant().Evaluate(
            WithOutcome(new AgentOutcome { Success = true, Capability = "echo", Result = " " }));

        Assert.False(result.Passed);
        Assert.Equal("Outcome is empty.", result.Reason);
    }

    [Fact]
    public void Evaluate_MissingOutcome_Rejects()
    {
        var result = new NonEmptyOutcomeInvariant().Evaluate(WithOutcome(null!));

        Assert.False(result.Passed);
    }

    [Fact]
    public async Task RunAsync_FailedCommand_ReportsOnlyTheRealError()
    {
        var fixture = TestFixture.Create();

        var context = await fixture.Runtime.RunAsync("calc 10 20");

        Assert.Equal(TransitionStatus.Rejected, context.Transition.Status);
        Assert.Contains("SuccessOutcomeInvariant", context.Transition.Reason);
        Assert.DoesNotContain("NonEmptyOutcomeInvariant", context.Transition.Reason);
        Assert.DoesNotContain("Outcome is empty", context.Transition.Reason);
    }
}

using InvariantAgent.Core.Drift;
using InvariantAgent.Core.Model.Agent;
using InvariantAgent.Core.Model.Transition;
using InvariantAgent.Safety.Invariants.Identity;
using Xunit;

namespace InvariantAgent.Tests.Governance;

public sealed class BehaviouralDriftBoundInvariantTests
{
    [Fact]
    public void Evaluate_WithBoundedGoalChange_AllowsTransition()
    {
        var invariant = new BehaviouralDriftBoundInvariant(
            new BehaviouralDriftDetector());

        var context = new TransitionContext
        {
            Transition = new Transition
            {
                Before = new AgentState(),
                After = new AgentState
                {
                    Memory =
                    {
                        ["goal"] = "ship behavioural drift"
                    }
                }
            }
        };

        var result = invariant.Evaluate(context);

        Assert.True(result.Passed);
    }

    [Fact]
    public void Evaluate_WithUnboundedAdaptiveChange_RejectsTransition()
    {
        var invariant = new BehaviouralDriftBoundInvariant(
            new BehaviouralDriftDetector());

        var context = new TransitionContext
        {
            Transition = new Transition
            {
                Before = new AgentState(),
                After = new AgentState
                {
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
                }
            }
        };

        var result = invariant.Evaluate(context);

        Assert.False(result.Passed);
        Assert.Contains("Behavioural drift score", result.Reason);
        Assert.Contains("exceeds bound", result.Reason);
    }
}

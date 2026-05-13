using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Agent;
using InvariantAgent.Core.Model.Capability;
using InvariantAgent.Core.Model.Transition;

namespace InvariantAgent.Execution.Engine;

public class CapabilityExecutor : IExecutor
{
    private readonly ICapabilityRegistry _capabilities;

    public CapabilityExecutor(ICapabilityRegistry capabilities)
    {
        _capabilities = capabilities;
    }

    public void Execute(TransitionContext context)
    {
        var transition = context.Transition;

        var action = transition.ProposedAction;

        if (action == null)
        {
            transition.Status = TransitionStatus.Failed;

            transition.Outcome = new AgentOutcome
            {
                Success = false,
                Error = "No proposed action."
            };

            return;
        }

        var result = ExecuteCapability(action);

        transition.Outcome = new AgentOutcome
        {
            Success = result.Success,
            Capability = result.Capability,
            Result = result.Success ? result.Data?.ToString() ?? "" : "",
            Error = result.Error
        };

        transition.SelfModification = result.ProposedModification;

        transition.Status = TransitionStatus.Executed;

        if (transition.SelfModification != null)
        {
            transition.AddEvent(
                TransitionEventStage.SelfModification,
                $"{transition.SelfModification.Target}.{transition.SelfModification.Operation} {transition.SelfModification.Key}");
        }
    }

    private CapabilityResult ExecuteCapability(
    AgentAction action)
    {
        var capability = _capabilities.Get(action.Capability);

        if (capability == null)
        {
            return new CapabilityResult
            {
                Success = false,
                Capability = action.Capability,
                Error = $"Unknown capability '{action.Capability}'."
            };
        }

        return capability.Execute(new CapabilityRequest { Input = action.Input });
    }
}
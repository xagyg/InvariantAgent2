using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Agent;
using InvariantAgent.Core.Model.Capability;
using InvariantAgent.Core.Model.Transition;

namespace InvariantAgent.Execution.Engine;

/// <summary>
/// Default <see cref="IExecutor"/> (IBAAM Execution layer). Resolves the proposed capability, executes
/// it and records the outcome plus any proposed self-modification on the transition.
/// </summary>
/// <remarks>
/// The executor mediates capability access but never applies self-modifications; those are evaluated
/// by invariants and assimilated by the reducer. Exceptions thrown by capabilities are converted
/// into failed results, so the transition is rejected by execution invariants and still persisted
/// instead of crashing the runtime.
/// </remarks>
public class CapabilityExecutor : IExecutor
{
    private readonly ICapabilityRegistry _capabilities;

    /// <summary>Creates the executor.</summary>
    /// <param name="capabilities">Registry used to resolve capability names.</param>
    public CapabilityExecutor(ICapabilityRegistry capabilities)
    {
        _capabilities = capabilities;
    }

    /// <inheritdoc />
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

        // Record the proposal only; SelfModification invariants decide whether it is applied.
        transition.SelfModification = result.ProposedModification;

        transition.Status = TransitionStatus.Executed;

        if (transition.SelfModification != null)
        {
            transition.AddEvent(
                TransitionEventStage.SelfModification,
                $"{transition.SelfModification.Target}.{transition.SelfModification.Operation} {transition.SelfModification.Key}");
        }
    }

    // Resolves and invokes the capability. An unknown capability or a capability that throws
    // yields a failed result rather than an exception.
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

        try
        {
            return capability.Execute(new CapabilityRequest { Input = action.Input });
        }
        catch (Exception ex)
        {
            return CapabilityResult.Fail(
                action.Capability,
                $"Capability '{action.Capability}' failed: {ex.Message}");
        }
    }
}
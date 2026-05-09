using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model;

namespace InvariantAgent.Execution.Engine;

public class CapabilityExecutor : IExecutor
{
    private readonly ICapabiltyRegistry _capabilities;

    public CapabilityExecutor(ICapabiltyRegistry capabilities)
    {
        _capabilities = capabilities;
    }

    public AgentOutcome Execute(AgentAction action, AgentState state)
    {
        var capabilities = _capabilities.Get(action.Capability);

        if (capabilities == null)
        {
            return new AgentOutcome
            {                
                Capability = action.Capability,
                Input = action.Input,
                Result = CapabilityResult.Fail(action.Capability, "Unkown tool or service"),
                StateVersion = state.Version
            };           
        }

        CapabilityResult result;

        try
        {
            var raw = capabilities.Execute(new CapabilityRequest { Input = action.Input }, state);

            result = CapabilityResult.Ok(action.Capability, raw.Data);
        }
        catch (Exception ex)
        {
            result = CapabilityResult.Fail(action.Capability, ex.Message);
        }

        return new AgentOutcome
        {
            Capability = action.Capability,
            Input = action.Input,
            Result = result,
            StateVersion = state.Version
        };
    }
}
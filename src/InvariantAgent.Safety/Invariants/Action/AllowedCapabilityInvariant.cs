using InvariantAgent.Core.Model;
using InvariantAgent.Core.Abstractions;

namespace InvariantAgent.Safety.Invariants.Action;

public class AllowedCapabilityInvariant : IActionInvariant
{
    public string Name => nameof(AllowedCapabilityInvariant);

    private readonly HashSet<string> _allowedCapabilities;

    public AllowedCapabilityInvariant(IEnumerable<string> allowedCapabilities)
    {
        _allowedCapabilities = new HashSet<string>(allowedCapabilities);
    }

    public InvariantResult Evaluate(AgentAction action)
    {
        if (_allowedCapabilities.Contains(action.Capability))
        {
            return new InvariantResult
            {
                IsValid = true,
                Reason = null,
                InvariantName = Name
            };
        }

        return new InvariantResult
        {
            IsValid = false,
            Reason = $"Capability '{action.Capability}' is not in the allowed set of tools or services",
            InvariantName = Name
        };
    }
}
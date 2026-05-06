using InvariantAgent.Core.Model;
using InvariantAgent.Core.Abstractions;

namespace InvariantAgent.Safety.Invariants.Action;

public class AllowedToolsInvariant : IActionInvariant
{
    private readonly HashSet<string> _allowedTools;

    public AllowedToolsInvariant(IEnumerable<string> allowedTools)
    {
        _allowedTools = new HashSet<string>(allowedTools);
    }

    public InvariantResult Evaluate(AgentAction action)
    {
        if (_allowedTools.Contains(action.Tool))
        {
            return new InvariantResult
            {
                IsValid = true,
                Reason = null
            };
        }

        return new InvariantResult
        {
            IsValid = false,
            Reason = $"Tool '{action.Tool}' is not in allowed set"
        };
    }
}
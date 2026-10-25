using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Capability;
using InvariantAgent.Core.Model.Data;
using InvariantAgent.Core.Model.Transition;
using InvariantAgent.Core.Rendering;
using System.Text;

namespace InvariantAgent.Capabilities.Tools.Internal;

/// <summary>
/// <c>explain [last | id-prefix]</c>: explains a transition's plan, invariant decisions and state impact.
/// </summary>
public sealed class ExplainTool : ICapability
{
    private readonly ITransitionStore _store;

    /// <inheritdoc />
    public string Name => "explain";

    /// <summary>Creates the tool.</summary>
    /// <param name="store">Transition history source.</param>
    public ExplainTool(ITransitionStore store)
    {
        _store = store;
    }

    /// <inheritdoc />
    public CapabilityResult Execute(CapabilityRequest request)
    {
        var input = request.Input;

        var transitions = _store.GetAll();

        if (transitions.Count == 0)
        {
            return CapabilityResult.Ok(Name, new TextData { Value = "No transitions recorded." });
        }

        Transition? transition = ResolveTransition(transitions, input);

        if (transition == null)
        {
            return CapabilityResult.Fail(Name, "Transition not found.");
        }

        return CapabilityResult.Ok(
            Name,
            new TextData
            {
                Value = TransitionFormatter.FormatExplain(transition)
            });
    }

    // Empty input or "last" selects the latest transition; otherwise matches by Id prefix.
    // The explain transition that is currently running is not stored yet, so "last" means the previous transition.
    private static Transition? ResolveTransition(
        IReadOnlyList<Transition> transitions,
        string input)
    {
        if (string.IsNullOrWhiteSpace(input) || input.Equals("last", StringComparison.OrdinalIgnoreCase))
        {
            return transitions.LastOrDefault();
        }

        return transitions.LastOrDefault(t => t.Id.ToString().StartsWith(input, StringComparison.OrdinalIgnoreCase));
    }
}
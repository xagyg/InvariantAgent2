using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Capability;
using InvariantAgent.Core.Model.Data;
using InvariantAgent.Core.Model.Transition;
using InvariantAgent.Core.Rendering;
using System.Text;

public sealed class ExplainTool : ICapability
{
    private readonly ITransitionStore _store;

    public string Name => "explain";

    public ExplainTool(ITransitionStore store)
    {
        _store = store;
    }

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
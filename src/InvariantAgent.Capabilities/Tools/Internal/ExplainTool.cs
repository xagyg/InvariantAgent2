using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Capability;
using InvariantAgent.Core.Model.Data;
using InvariantAgent.Core.Model.Transition;
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

        var sb = new StringBuilder();

        sb.AppendLine("\n==== EXPLAIN START ====");
        sb.AppendLine();

        sb.AppendLine($"Transition={transition.Id}");
        sb.AppendLine($"Status={transition.Status}");
        sb.AppendLine($"Input={transition.Input}");

        sb.AppendLine();

        sb.AppendLine("Decision Path:");

        foreach (var e in transition.Events)
        {
            sb.Append(" - ")
              .Append(e.Stage)
              .Append(": ")
              .AppendLine(e.Message);
        }

        if (!string.IsNullOrWhiteSpace(transition.Reason))
        {
            sb.AppendLine();
            sb.AppendLine($"Reason={transition.Reason}");
        }

        if (transition.SelfModification != null)
        {
            sb.AppendLine();
            sb.AppendLine("Self Modification:");

            sb.AppendLine(
                $"  {transition.SelfModification.Target}." +
                $"{transition.SelfModification.Operation} " +
                $"{transition.SelfModification.Key}");
        }

        if (transition.Before != null &&
            transition.After != null)
        {
            sb.AppendLine();
            sb.AppendLine(
                $"State Transition: " +
                $"{transition.Before.Version} -> " +
                $"{transition.After.Version}");
        }

        sb.AppendLine();
        sb.AppendLine("==== EXPLAIN END ====");

        return CapabilityResult.Ok(Name, new TextData { Value = sb.ToString() });
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
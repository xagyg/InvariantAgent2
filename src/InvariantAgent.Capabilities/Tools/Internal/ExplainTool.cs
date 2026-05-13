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

        return RenderExplanation(transition);

    }
    private CapabilityResult RenderExplanation(Transition transition)
    {
        var sb = new StringBuilder();

        sb.AppendLine("\n==== EXPLAIN START ====");
        sb.AppendLine();

        sb.AppendLine($"Input: {transition.Input}");
        sb.AppendLine($"Outcome: {transition.Status}");
        sb.AppendLine();

        AppendNaturalSummary(sb, transition);
        AppendPlanning(sb, transition);
        AppendGovernance(sb, transition);
        AppendStateImpact(sb, transition);

        sb.AppendLine();
        sb.AppendLine("==== EXPLAIN END ====");


        return CapabilityResult.Ok(
            Name,
            new TextData
            {
                Value = sb.ToString()
            });
    }

    private static void AppendNaturalSummary(StringBuilder sb, Transition transition)
    {
        sb.AppendLine("Summary:");

        var capability = transition.ProposedAction?.Capability;

        if (transition.Status == TransitionStatus.Rejected)
        {
            sb.AppendLine(
                $"  The runtime planned to use '{capability}', but the transition was rejected before execution.");

            if (!string.IsNullOrWhiteSpace(transition.Reason))
                sb.AppendLine($"  {transition.Reason}");
        }
        else if (transition.Status == TransitionStatus.Completed)
        {
            sb.AppendLine(
                $"  The runtime used '{capability}' and completed the transition successfully.");
        }
        else
        {
            sb.AppendLine(
                $"  The runtime planned to use '{capability}' and ended with status '{transition.Status}'.");
        }

        sb.AppendLine();
    }

    private static void AppendPlanning(StringBuilder sb, Transition transition)
    {
        sb.AppendLine("Plan:");

        if (transition.ProposedAction == null)
        {
            sb.AppendLine("  No action was proposed.");
        }
        else
        {
            sb.AppendLine($"  Capability: {transition.ProposedAction.Capability}");

            if (!string.IsNullOrWhiteSpace(transition.ProposedAction.Input))
                sb.AppendLine($"  Input: {transition.ProposedAction.Input}");
        }

        sb.AppendLine();
    }

    private static void AppendGovernance(StringBuilder sb, Transition transition)
    {
        var preInvariantEvents = transition.Events
            .Where(e => e.Stage == TransitionEventStage.PreInvariant)
            .ToList();

        var postInvariantEvents = transition.Events
            .Where(e => e.Stage == TransitionEventStage.PostInvariant)
            .ToList();

        if (preInvariantEvents.Count == 0 && postInvariantEvents.Count == 0)
        {
            return;
        }

        sb.AppendLine("Governance:");

        if (preInvariantEvents.Count > 0)
        {
            sb.AppendLine("  Pre-Execution:");

            foreach (var e in preInvariantEvents)
            {
                AppendInvariantEvent(sb, e.Message);
            }

            sb.AppendLine();
        }

        if (postInvariantEvents.Count > 0)
        {
            sb.AppendLine("  Post-Execution:");

            foreach (var e in postInvariantEvents)
            {
                AppendInvariantEvent(sb, e.Message);
            }

            sb.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(transition.Reason))
        {
            sb.AppendLine("Decision:");
            sb.AppendLine($"  {transition.Reason}");
            sb.AppendLine();
        }
    }

    private static void AppendInvariantEvent(
        StringBuilder sb,
        string message)
    {
        var passed = message.Contains(
            "Passed",
            StringComparison.OrdinalIgnoreCase);

        if (passed)
        {
            sb.AppendLine(
                $"    [Pass] {message.Replace(": Passed", "")}");
        }
        else
        {
            sb.AppendLine($"    [Fail] {message}");
        }
    }

    private static void AppendStateImpact(StringBuilder sb, Transition transition)
    {
        sb.AppendLine("State Impact:");

        if (transition.After == null)
        {
            sb.AppendLine("  No state change was applied.");
            return;
        }

        var beforeVersion = transition.Before?.Version.ToString() ?? "?";
        var afterVersion = transition.After.Version.ToString();

        sb.AppendLine($"  Version changed from {beforeVersion} to {afterVersion}.");

        if (transition.SelfModification != null)
        {
            sb.AppendLine(
                $"  Self modification: {transition.SelfModification.Target}.{transition.SelfModification.Operation} {transition.SelfModification.Key}");
        }
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
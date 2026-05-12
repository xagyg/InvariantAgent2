using System.Text;
using InvariantAgent.Core.Model.Agent;

namespace InvariantAgent.Adaptive
{
    public abstract class LlmPlanner : Planner
    {
        protected override AgentAction GeneratePlan(
            StateProjection state,
            string input)
        {
            var prompt = BuildPrompt(state, input);

            var response = Complete(prompt);

            return ParseAction(response);
        }

        protected abstract string Complete(string prompt);

        //        protected virtual string BuildPrompt(StateProjection state, string input)
        //        {
        //            var sb = new StringBuilder();

        //            sb.AppendLine("""
        //You are an agent planner.

        //Available capabilities (tools or services):
        //- echo
        //- search
        //- calculator
        //- replay

        //Return ONLY:

        //capability: <capability-name>
        //input: <capability-input>

        //User input:
        //""");

        //            sb.AppendLine(input);

        //            return sb.ToString();
        //        }

        protected virtual string BuildPrompt(StateProjection state, string input)
        {
            var sb = new StringBuilder();

            sb.AppendLine("""
You are a governed agent planner.

Your role is to select the single best capability
and input for the current request.

Available capabilities:
- echo
- search
- calculator
- replay
- drift
- memory-set
- memory-show

You operate under invariant governance.

Return ONLY the following format:

capability: <capability-name>
input: <capability-input>

Do not explain your reasoning.
Do not return markdown.
Do not return JSON.
""");

            sb.AppendLine();

            sb.AppendLine("Current runtime state:");

            sb.AppendLine($"Version: {state.Version}");

            if (!string.IsNullOrWhiteSpace(state.Goal))
            {
                sb.AppendLine($"Goal: {state.Goal}");
            }

            if (!string.IsNullOrWhiteSpace(state.MemorySummary))
            {
                sb.AppendLine($"Memory: {state.MemorySummary}");
            }

            if (!string.IsNullOrWhiteSpace(state.LastOutcome))
            {
                sb.AppendLine($"Last outcome: {state.LastOutcome}");
            }

            if (state.ActivePolicies.Count > 0)
            {
                sb.AppendLine(
                    $"Policies: {string.Join(", ", state.ActivePolicies)}");
            }

            sb.AppendLine();

            sb.AppendLine("User input:");

            sb.AppendLine(input);

            return sb.ToString();
        }

        protected virtual AgentAction ParseAction(string text)
        {
            var lines = text
                .Split('\n')
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            var capability = lines
                .First(x => x.StartsWith("capability:"))
                .Substring("capability:".Length)
                .Trim();

            var input = lines
                .First(x => x.StartsWith("input:"))
                .Substring("input:".Length)
                .Trim();

            return new AgentAction
            {
                Capability = capability,
                Input = input
            };
        }
    }
}
using System.Text;
using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model;

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

        protected virtual string BuildPrompt(
            StateProjection state,
            string input)
        {
            var sb = new StringBuilder();

            sb.AppendLine("""
You are an agent planner.

Available tools:
- echo
- search
- calculator
- replay

Return ONLY:

tool: <tool-name>
input: <tool-input>

User input:
""");

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

            var tool = lines
                .First(x => x.StartsWith("tool:"))
                .Substring("tool:".Length)
                .Trim();

            var input = lines
                .First(x => x.StartsWith("input:"))
                .Substring("input:".Length)
                .Trim();

            return new AgentAction
            {
                Tool = tool,
                Input = input
            };
        }
    }
}
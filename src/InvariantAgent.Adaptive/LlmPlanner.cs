using System.Text;
using InvariantAgent.Core.Model.Agent;
using InvariantAgent.Core.Model.Planning;

namespace InvariantAgent.Adaptive
{
    /// <summary>
    /// Base class for LLM-backed planners. Builds a prompt from the bounded state projection, sends it to
    /// the model and parses a <c>capability:</c> / <c>input:</c> response into an <see cref="AgentAction"/>.
    /// </summary>
    /// <remarks>
    /// The LLM is treated as an untrusted cognitive generator: whatever it returns is still subject to
    /// full invariant governance before execution.
    /// </remarks>
    public abstract class LlmPlanner : Planner
    {
        /// <inheritdoc />
        protected override AgentAction GeneratePlan(PlannerContext context)
        {
            var prompt = BuildPrompt(context.State, context.Input);

            if (context.Revision != null)
            {
                prompt += BuildRevisionPrompt(context.Revision);
            }

            var response = Complete(prompt);

            return ParseAction(response);
        }

        /// <summary>Sends the prompt to the underlying model and returns its raw text response.</summary>
        /// <param name="prompt">The fully constructed planner prompt.</param>
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

        /// <summary>
        /// Builds the planner prompt: role, available capabilities, required output format, the projected
        /// runtime state and the user input.
        /// </summary>
        /// <param name="state">Bounded state projection <c>π(Sₜ)</c>.</param>
        /// <param name="input">User input.</param>
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

        /// <summary>
        /// Appends the conscience's appraisal of the previous proposal (IBAAM 6, Section 6.3), so the model can re-plan
        /// around the concern rather than only being blocked by it.
        /// </summary>
        /// <param name="revision">Conscience feedback on the previous proposal.</param>
        protected virtual string BuildRevisionPrompt(PlannerRevision revision)
        {
            var sb = new StringBuilder();

            sb.AppendLine();
            sb.AppendLine("Conscience feedback on your previous proposal:");
            sb.AppendLine($"Previous capability: {revision.Previous?.Capability}");
            sb.AppendLine($"Previous input: {revision.Previous?.Input}");

            if (revision.ValuesAtStake.Count > 0)
            {
                sb.AppendLine($"Values at stake: {string.Join(", ", revision.ValuesAtStake)}");
            }

            sb.AppendLine($"Appraisal: {revision.Justification}");
            sb.AppendLine();
            sb.AppendLine("""
Propose a different action that still serves the user's request but addresses this concern.
If no such action exists, propose the most conservative option (for example echo an explanation).
Use the same output format.
""");

            return sb.ToString();
        }

        /// <summary>Parses the model response into an action.</summary>
        /// <param name="text">Raw model output containing <c>capability:</c> and <c>input:</c> lines.</param>
        /// <exception cref="InvalidOperationException">
        /// Either line is missing. <see cref="Planner"/> turns this into an error action that is then rejected.
        /// </exception>
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
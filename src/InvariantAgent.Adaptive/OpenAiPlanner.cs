using OpenAI.Chat;

namespace InvariantAgent.Adaptive
{
    /// <summary>LLM planner backed by the OpenAI Chat Completions API.</summary>
    public class OpenAiPlanner : LlmPlanner
    {
        public override string Name => "openai";

        // Configured chat client (model is fixed in the constructor).
        private readonly ChatClient _client;

        /// <summary>Creates the planner.</summary>
        /// <param name="apiKey">OpenAI API key.</param>
        public OpenAiPlanner(string apiKey)
        {
            _client = new ChatClient(
                model: "gpt-5",
                apiKey: apiKey);
        }

        /// <inheritdoc />
        protected override string Complete(string prompt)
        {
            var response = _client.CompleteChat(prompt);

            return response.Value.ToString();
        }
    }
}
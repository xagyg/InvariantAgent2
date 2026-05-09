using OpenAI.Chat;

namespace InvariantAgent.Adaptive
{
    public class OpenAiPlanner : LlmPlanner
    {
        private readonly ChatClient _client;

        public OpenAiPlanner(string apiKey)
        {
            _client = new ChatClient(
                model: "gpt-5",
                apiKey: apiKey);
        }

        protected override string Complete(string prompt)
        {
            var response = _client.CompleteChat(prompt);

            return response.Value.ToString();
        }
    }
}
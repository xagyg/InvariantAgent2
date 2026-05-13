using GenerativeAI;

namespace InvariantAgent.Adaptive
{
    public class GeminiPlanner : LlmPlanner
    {
        public override string Name => "gemini";

        private readonly GenerativeModel _model;

        public GeminiPlanner(string apiKey)
        {
            var googleAI = new GoogleAi(apiKey);

            _model = googleAI.CreateGeminiModel(
                modelName: "gemini-2.5-pro");
        }

        protected override string Complete(string prompt)
        {
            var response = _model
                .GenerateContentAsync(prompt)
                .GetAwaiter()
                .GetResult();

            return response.Text;
        }
    }
}
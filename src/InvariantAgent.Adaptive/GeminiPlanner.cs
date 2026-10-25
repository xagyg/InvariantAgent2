using GenerativeAI;

namespace InvariantAgent.Adaptive
{
    /// <summary>LLM planner backed by Google Gemini.</summary>
    public class GeminiPlanner : LlmPlanner
    {
        public override string Name => "gemini";

        // Configured Gemini model (model name is fixed in the constructor).
        private readonly GenerativeModel _model;

        /// <summary>Creates the planner.</summary>
        /// <param name="apiKey">Google AI API key.</param>
        public GeminiPlanner(string apiKey)
        {
            var googleAI = new GoogleAi(apiKey);

            _model = googleAI.CreateGeminiModel(
                modelName: "gemini-2.5-pro");
        }

        /// <inheritdoc />
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
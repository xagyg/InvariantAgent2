using InvariantAgent.Core.Model;


namespace InvariantAgent.Adaptive
{

    public class Planner
    {
        public AgentAction Plan(StateProjection state, string input)
        {
            // very simple deterministic policy (for now)
            return new AgentAction
            {
                Tool = ResolveTool(state, input),
                Input = input
            };
        }

        private string ResolveTool(StateProjection state, string input)
        {
            if (input.Contains("calculate"))
                return "calculator";

            if (input.Contains("search"))
                return "search";

            return "echo";
        }
    }
}

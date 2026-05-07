using InvariantAgent.Core.Model;
using System;


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
            if (input.Trim().StartsWith("calculate"))
                return "calculator";

            if (input.Trim().StartsWith("search"))
                return "search";

            if (input.Trim().StartsWith("echo"))
                return "echo";

            throw new InvalidOperationException($"Unknown tool: {input.Split(' ')[0]}");
        }
    }
}

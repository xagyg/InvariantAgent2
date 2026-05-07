using InvariantAgent.Core.Model;
using InvariantAgent.Core.Parsing;
using System;


namespace InvariantAgent.Adaptive
{

    public class Planner
    {
        public AgentAction Plan(StateProjection state, string input)
        {
            // very simple deterministic policy (for now)
            return AgentActionParser.Parse(input);
        }
    }
}

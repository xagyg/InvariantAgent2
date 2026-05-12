using InvariantAgent.Core.Model.Agent;
using InvariantAgent.Core.Parsing;

namespace InvariantAgent.Adaptive
{
    public sealed class CommandPlanner : Planner
    {
        protected override AgentAction GeneratePlan(StateProjection state, string input)
        {
            return AgentActionParser.Parse(input);
        }
    }
}
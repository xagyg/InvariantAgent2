using InvariantAgent.Core.Model.Agent;
using InvariantAgent.Core.Parsing;

namespace InvariantAgent.Adaptive
{
    public sealed class CommandPlanner : Planner
    {
        public override string Name => "command";

        protected override AgentAction GeneratePlan(StateProjection state, string input)
        {
            return AgentActionParser.Parse(input);
        }
    }
}
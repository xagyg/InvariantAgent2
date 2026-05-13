using InvariantAgent.Core.Model.Agent;
using InvariantAgent.Core.Parsing;

namespace InvariantAgent.Adaptive
{
    public class RulePlanner : Planner
    {
        public override string Name => "rule";

        protected override AgentAction GeneratePlan(StateProjection state, string input)
        {
            return AgentActionParser.Parse(input);
        }
    }
}
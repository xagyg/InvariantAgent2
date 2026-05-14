using InvariantAgent.Core.Model.Agent;
using InvariantAgent.Core.Model.Planning;
using InvariantAgent.Core.Parsing;

namespace InvariantAgent.Adaptive
{
    public class RulePlanner : Planner
    {
        public override string Name => "rule";

        protected override AgentAction GeneratePlan(PlannerContext context)
        {
            return AgentActionParser.Parse(context.Input);
        }
    }
}
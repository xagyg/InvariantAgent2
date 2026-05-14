using InvariantAgent.Core.Model.Agent;
using InvariantAgent.Core.Model.Planning;
using InvariantAgent.Core.Parsing;

namespace InvariantAgent.Adaptive
{
    public sealed class CommandPlanner : Planner
    {
        public override string Name => "command";

        protected override AgentAction GeneratePlan(PlannerContext context)
        {
            return AgentActionParser.Parse(context.Input);
        }
    }
}
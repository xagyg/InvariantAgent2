using InvariantAgent.Core.Model.Agent;
using InvariantAgent.Core.Model.Planning;
using InvariantAgent.Core.Parsing;

namespace InvariantAgent.Adaptive
{
    /// <summary>
    /// Rule-based planner placeholder. Currently identical to <see cref="CommandPlanner"/>; intended as
    /// an extension point for deterministic rule-based action selection.
    /// </summary>
    public class RulePlanner : Planner
    {
        /// <inheritdoc />
        public override string Name => "rule";

        /// <inheritdoc />
        protected override AgentAction GeneratePlan(PlannerContext context)
        {
            return AgentActionParser.Parse(context.Input);
        }
    }
}
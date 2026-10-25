using InvariantAgent.Core.Model.Agent;
using InvariantAgent.Core.Model.Planning;
using InvariantAgent.Core.Parsing;

namespace InvariantAgent.Adaptive
{
    /// <summary>
    /// Deterministic planner that treats input as a literal command (<c>capability arguments</c>).
    /// The default planner; useful for reproducible runs, tests and replay.
    /// </summary>
    public sealed class CommandPlanner : Planner
    {
        /// <inheritdoc />
        public override string Name => "command";

        /// <inheritdoc />
        protected override AgentAction GeneratePlan(PlannerContext context)
        {
            return AgentActionParser.Parse(context.Input);
        }
    }
}
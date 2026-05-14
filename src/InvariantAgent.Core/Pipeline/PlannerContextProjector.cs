using InvariantAgent.Core.Model.Agent;
using InvariantAgent.Core.Model.Planning;
using System.Linq;

namespace InvariantAgent.Core.Pipeline;

public static class PlannerContextProjector
{
    public static PlannerContext Project(AgentState state, string input)
    {
        return new PlannerContext
        {
            Input = input,

            State = StateProjector.Project(state),

            Goal = state.Memory.TryGetValue("goal", out var goal) ? goal?.ToString() ?? "" : "",

            LastOutcome = state.Memory.TryGetValue("lastOutcome", out var outcome) ? outcome?.ToString() ?? "" : "",

            RelevantMemory = state.Memory
                .Where(kvp => kvp.Key == "goal" || kvp.Key == "lastOutcome")
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
        };
    }
}
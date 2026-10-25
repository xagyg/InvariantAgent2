using InvariantAgent.Core.Model.Agent;
using InvariantAgent.Core.Model.Planning;
using System.Linq;

namespace InvariantAgent.Core.Pipeline;

/// <summary>
/// Builds the <see cref="PlannerContext"/> handed to planners, enforcing the bounded state
/// projection <c>π(Sₜ)</c> rather than exposing full runtime state.
/// </summary>
public static class PlannerContextProjector
{
    /// <summary>Projects <paramref name="state"/> and <paramref name="input"/> into a planner context.</summary>
    /// <param name="state">The current committed agent state.</param>
    /// <param name="input">Raw user input.</param>
    /// <returns>A planner context exposing only whitelisted memory (<c>goal</c>, <c>lastOutcome</c>).</returns>
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
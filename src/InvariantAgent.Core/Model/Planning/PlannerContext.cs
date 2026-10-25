using InvariantAgent.Core.Model.Agent;
using System.Collections.Generic;

namespace InvariantAgent.Core.Model.Planning;

/// <summary>
/// Everything a planner is allowed to see: user input plus the bounded state projection <c>π(Sₜ)</c>.
/// </summary>
public sealed class PlannerContext
{
    /// <summary>Raw user input <c>Iₜ</c>.</summary>
    public string Input { get; init; } = "";

    /// <summary>Bounded projection of runtime state.</summary>
    public StateProjection State { get; init; } = new();

    /// <summary>The <c>goal</c> memory value, if any.</summary>
    public string Goal { get; init; } = "";

    /// <summary>The <c>lastOutcome</c> memory value, if any.</summary>
    public string LastOutcome { get; init; } = "";

    /// <summary>Whitelisted memory entries exposed to the planner.</summary>
    public IReadOnlyDictionary<string, object> RelevantMemory { get; init; } = new Dictionary<string, object>();
}
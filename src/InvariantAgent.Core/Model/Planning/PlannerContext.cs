using InvariantAgent.Core.Model.Agent;
using System.Collections.Generic;

namespace InvariantAgent.Core.Model.Planning;

public sealed class PlannerContext
{
    public string Input { get; init; } = "";

    public StateProjection State { get; init; } = new();

    public string Goal { get; init; } = "";

    public string LastOutcome { get; init; } = "";

    public IReadOnlyDictionary<string, object> RelevantMemory { get; init; } = new Dictionary<string, object>();
}
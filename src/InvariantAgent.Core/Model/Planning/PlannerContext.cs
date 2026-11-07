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

    /// <summary>
    /// Set when the conscience has asked for a revised plan: the appraisal of the previous proposal, returned to the
    /// planner as context (IBAAM 6, Section 6.3). <c>null</c> for a first proposal.
    /// </summary>
    public PlannerRevision Revision { get; init; }
}

/// <summary>
/// Conscience feedback handed back to the planner with a request to re-plan. This is what makes tension a cost on action
/// selection rather than only a gate.
/// </summary>
public sealed class PlannerRevision
{
    /// <summary>Revision attempt being requested (1 for the first re-plan).</summary>
    public int Attempt { get; init; }

    /// <summary>The proposal the conscience asked to revise.</summary>
    public AgentAction Previous { get; init; }

    /// <summary>Combined tension of the previous proposal.</summary>
    public double Tension { get; init; }

    /// <summary>The conscience's justification, generated from its tension components.</summary>
    public string Justification { get; init; } = "";

    /// <summary>Protected values the previous proposal put at stake.</summary>
    public IReadOnlyList<string> ValuesAtStake { get; init; } = System.Array.Empty<string>();
}
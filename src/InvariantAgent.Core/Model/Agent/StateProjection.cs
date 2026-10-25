using System.Collections.Generic;

namespace InvariantAgent.Core.Model.Agent
{
    /// <summary>
    /// The bounded projection <c>π(Sₜ)</c> of runtime state exposed to planners.
    /// </summary>
    /// <remarks>
    /// Planners never see the full <see cref="AgentState"/>; this controlled interface limits what
    /// adaptive cognition can observe about governed execution.
    /// </remarks>
    public sealed class StateProjection
    {
        /// <summary>Current state version.</summary>
        public int Version { get; init; }

        /// <summary>Current explicit goal.</summary>
        public string Goal { get; init; } = "";

        /// <summary>Summary of a whitelisted subset of memory keys.</summary>
        public string MemorySummary { get; init; } = "";

        /// <summary>Result of the last successful non-introspective capability.</summary>
        public string LastOutcome { get; init; } = "";

        /// <summary>Currently active policies.</summary>
        public IReadOnlyList<string> ActivePolicies { get; init; } = new List<string>();
    }
}
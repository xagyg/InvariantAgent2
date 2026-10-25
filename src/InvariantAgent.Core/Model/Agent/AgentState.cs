using System.Collections.Generic;

namespace InvariantAgent.Core.Model.Agent
{
    /// <summary>
    /// The agent's runtime state <c>Sₜ</c>. A new instance is produced by the reducer for each
    /// committed transition, and only committed after reduction invariants pass.
    /// </summary>
    public class AgentState
    {
        /// <summary>Monotonic state version; incremented by one on every committed transition.</summary>
        public int Version { get; set; } = 0;
        /// <summary>Adaptive key/value memory (e.g. <c>goal</c>, <c>lastOutcome</c>). Changed via governed self-modification.</summary>
        public Dictionary<string, object> Memory { get; set; } = new();
        /// <summary>Domain events recorded against this state.</summary>
        public List<AgentEvent> Events { get; set; } = new();
        /// <summary>Current operating mode. Changes contribute to behavioural drift.</summary>
        public string Mode { get; set; } = "";
        /// <summary>Active behavioural policies. Changes contribute to behavioural drift.</summary>
        public List<string> Policies { get; set; } = new();

        /// <summary>Explicit goal. When empty, drift and stability logic fall back to the <c>goal</c> memory entry.</summary>
        public string Goal { get; set; }

        /// <summary>Appends a domain event.</summary>
        /// <param name="e">The event to record.</param>
        public void AddEvent(AgentEvent e)
        {
            Events.Add(e);
        }
    }
}

using System;
using System.Collections.Generic;

namespace InvariantAgent.Core.Model.Transition
{
    /// <summary>A single entry in a transition's audit trail.</summary>
    public sealed class TransitionEvent
    {
        /// <summary>UTC time of the event.</summary>
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;

        /// <summary>Pipeline stage that raised the event.</summary>
        public TransitionEventStage Stage { get; init; }

        /// <summary>Human-readable message (also printed by the REPL).</summary>
        public string Message { get; init; } = "";

        /// <summary>Structured data (e.g. invariant name, scope, pass/fail) used by observability tools.</summary>
        public Dictionary<string, object> Metadata { get; init; } = new();
    }
}
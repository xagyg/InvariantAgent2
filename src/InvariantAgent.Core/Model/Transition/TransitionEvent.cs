using System;
using System.Collections.Generic;

namespace InvariantAgent.Core.Model.Transition
{
    public sealed class TransitionEvent
    {
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;

        public TransitionEventStage Stage { get; init; }

        public string Message { get; init; } = "";

        public Dictionary<string, object> Metadata { get; init; } = new();
    }
}
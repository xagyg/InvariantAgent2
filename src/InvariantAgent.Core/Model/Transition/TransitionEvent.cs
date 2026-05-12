using System;

namespace InvariantAgent.Core.Model.Transition
{
    public sealed class TransitionEvent
    {
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;

        public string Stage { get; init; } = "";

        public string Message { get; init; } = "";
    }
}
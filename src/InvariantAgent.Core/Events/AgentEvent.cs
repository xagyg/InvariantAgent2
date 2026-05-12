using System;

namespace InvariantAgent.Core.Events;

public abstract class AgentEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public abstract string Type { get; }

    public abstract string ToObservation();
}
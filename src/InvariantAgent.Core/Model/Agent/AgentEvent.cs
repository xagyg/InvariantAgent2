using System;

namespace InvariantAgent.Core.Model.Agent;

/// <summary>
/// Base class for domain events recorded in <see cref="AgentState.Events"/>.
/// </summary>
public abstract class AgentEvent
{
    /// <summary>Unique event identifier.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();
    /// <summary>UTC time the event was created.</summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>Event type discriminator.</summary>
    public abstract string Type { get; }

    /// <summary>Renders the event as a short observation suitable for planner context.</summary>
    public abstract string ToObservation();
}
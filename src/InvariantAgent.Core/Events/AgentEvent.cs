using System;

namespace InvariantAgent.Core.Events;

public record AgentEvent(
    string SessionId,
    string Type,
    string Data,
    DateTime Timestamp
);
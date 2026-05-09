using System;

namespace InvariantAgent.Core.Model;

public class AgentOutcome
{
    // Which tool or eervice produced this outcome
    public string Capability { get; set; } = "";

    // Input passed to the tool or service
    public string Input { get; set; } = "";

    // Raw result of execution
    public CapabilityResult Result { get; set; }

    // Links outcome back to the state version that produced it
    public int StateVersion { get; set; }

    // Optional: timestamp for observability
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
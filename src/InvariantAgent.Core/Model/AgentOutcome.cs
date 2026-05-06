using System;

namespace InvariantAgent.Core.Model;

public class AgentOutcome
{
    // Which tool produced this outcome
    public string Tool { get; set; } = "";

    // Input passed to the tool (Aₜ payload)
    public string Input { get; set; } = "";

    // Raw result of execution (Oₜ)
    public object Result { get; set; }

    // Links outcome back to the state version that produced it
    public int StateVersion { get; set; }

    // Optional: timestamp for observability
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
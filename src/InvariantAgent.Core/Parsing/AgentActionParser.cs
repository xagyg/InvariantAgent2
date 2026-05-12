using InvariantAgent.Core.Model.Agent;
using System;

namespace InvariantAgent.Core.Parsing;
public static class AgentActionParser
{
    public static AgentAction Parse(string input)
    {
        var trimmed = input.Trim();

        var parts = trimmed.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);

        var capability = parts[0].ToLowerInvariant();
        var args = parts.Length > 1 ? parts[1] : "";

        return new AgentAction
        {
            Capability = capability,
            Input = args
        };
    }
}

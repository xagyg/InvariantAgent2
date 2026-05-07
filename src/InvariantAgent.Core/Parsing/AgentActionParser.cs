using InvariantAgent.Core.Model;
using System;

namespace InvariantAgent.Core.Parsing;
public static class AgentActionParser
{
    public static AgentAction Parse(string input)
    {
        var trimmed = input.Trim();

        var parts = trimmed.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);

        var tool = parts[0].ToLowerInvariant();
        var args = parts.Length > 1 ? parts[1] : "";

        return new AgentAction
        {
            Tool = tool,
            Input = args
        };
    }
}

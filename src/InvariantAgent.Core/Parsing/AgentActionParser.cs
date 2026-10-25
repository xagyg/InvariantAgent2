using InvariantAgent.Core.Model.Agent;
using System;

namespace InvariantAgent.Core.Parsing;
/// <summary>
/// Parses command-style input (<c>"capability arguments"</c>) into an <see cref="AgentAction"/>.
/// </summary>
public static class AgentActionParser
{
    /// <summary>
    /// Splits input on the first space: the first token (lower-cased) is the capability and the
    /// remainder is passed through unchanged as the capability input.
    /// </summary>
    /// <param name="input">Raw input, e.g. <c>calc 10+20</c>.</param>
    /// <returns>The parsed action.</returns>
    /// <remarks>Assumes non-empty input; the REPL filters blank lines before calling the runtime.</remarks>
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

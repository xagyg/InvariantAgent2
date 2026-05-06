using InvariantAgent.Core.Model;

namespace InvariantAgent.Core.Abstractions;
public interface ITool
{
    string Name { get; }

    object Run(string input, AgentState state);
}
using InvariantAgent.Core.Model;

namespace InvariantAgent.Core.Abstractions;

public interface IExecutor
{
    AgentOutcome Execute(AgentAction action, AgentState state);
}
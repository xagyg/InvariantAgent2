using InvariantAgent.Core.Model;

namespace InvariantAgent.Core.Abstractions
{
    public interface IControlOperator
    {
        AgentAction ApplyPre(AgentState state, AgentAction action);
        AgentOutcome ApplyPost(AgentState state, AgentOutcome outcome);
    }
}

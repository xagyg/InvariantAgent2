using InvariantAgent.Core.Model;
namespace InvariantAgent.Core.Abstractions
{
    public interface IStateReducer
    {
        AgentState Reduce(AgentState state, AgentOutcome outcome);
    }
}

using InvariantAgent.Core.Model;

namespace InvariantAgent.Core.Abstractions
{
    public interface IPlanner
    {
        AgentAction Plan(StateProjection state, string input);
    }
}

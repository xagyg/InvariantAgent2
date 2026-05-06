using InvariantAgent.Core.Model;
using InvariantAgent.Core.Control.Pre;

namespace InvariantAgent.Core.Abstractions
{
    public interface IPreControl
    {
        PreControlResult Evaluate(AgentState state, AgentAction action);
    }
}

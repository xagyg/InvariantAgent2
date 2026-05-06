using InvariantAgent.Core.Model;

namespace InvariantAgent.Core.Abstractions
{
    public interface ITransitionEngine
    {
        StateProjection Step(string input);
    }
}

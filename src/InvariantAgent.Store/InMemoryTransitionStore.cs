using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Transition;

namespace InvariantAgent.Storage
{
    public sealed class InMemoryTransitionStore : ITransitionStore
    {
        private readonly List<Transition> _transitions = new();

        public void Append(Transition transition)
        {
            _transitions.Add(transition);
        }

        public IReadOnlyList<Transition> GetAll()
        {
            return _transitions;
        }
    }
}
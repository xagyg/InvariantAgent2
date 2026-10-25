using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Transition;

namespace InvariantAgent.Storage
{
    /// <summary>Process-local <see cref="ITransitionStore"/>; history is lost when the process exits. The default store.</summary>
    public sealed class InMemoryTransitionStore : ITransitionStore
    {
        private readonly List<Transition> _transitions = new();

        /// <inheritdoc />
        public void Append(Transition transition)
        {
            _transitions.Add(transition);
        }

        /// <inheritdoc />
        public IReadOnlyList<Transition> GetAll()
        {
            return _transitions;
        }
    }
}
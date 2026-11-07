using System.Collections.Generic;
using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Conscience;

namespace InvariantAgent.Core.Conscience
{
    /// <summary>In-memory <see cref="ICommitmentStore"/> (tests and non-persistent runs).</summary>
    public sealed class InMemoryCommitmentStore : ICommitmentStore
    {
        private readonly List<Commitment> _commitments = new List<Commitment>();
        private readonly object _lock = new object();

        /// <inheritdoc />
        public void Append(Commitment commitment)
        {
            lock (_lock)
            {
                _commitments.Add(commitment);
            }
        }

        /// <inheritdoc />
        public IReadOnlyList<Commitment> GetAll()
        {
            lock (_lock)
            {
                return _commitments.ToArray();
            }
        }
    }
}

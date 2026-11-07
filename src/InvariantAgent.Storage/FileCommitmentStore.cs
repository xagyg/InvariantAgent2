using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Conscience;

namespace InvariantAgent.Storage
{
    /// <summary>
    /// Durable <see cref="ICommitmentStore"/>: every conscience commitment is appended to a JSONL file, so commitments
    /// outlive restarts and the file is also their audit history.
    /// </summary>
    /// <remarks>
    /// Commitments about individuals are sensitive in their own right (IBAAM 6, Section 9) and this file deserves the same
    /// protection as the transition history.
    /// </remarks>
    public sealed class FileCommitmentStore : ICommitmentStore
    {
        private readonly JsonLinesFile<Commitment> _file;
        private readonly List<Commitment> _commitments;
        private readonly object _lock = new();

        /// <summary>Opens the store and loads existing commitments.</summary>
        /// <param name="filePath">Path of the JSONL file.</param>
        public FileCommitmentStore(string filePath)
        {
            _file = new JsonLinesFile<Commitment>(filePath);
            _commitments = _file.ReadAll().ToList();
        }

        /// <summary>Full path of the backing file.</summary>
        public string FilePath => _file.FilePath;

        /// <inheritdoc />
        public void Append(Commitment commitment)
        {
            lock (_lock)
            {
                _file.Append(commitment);
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

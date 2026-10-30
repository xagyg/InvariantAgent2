using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Transition;

namespace InvariantAgent.Storage
{
    /// <summary>
    /// Durable <see cref="ITransitionStore"/> that appends each transition as one JSON line (JSONL).
    /// </summary>
    /// <remarks>
    /// Thread-safe within one process. Uses <c>InvariantAgentJson.Options</c>, so <c>object</c>-typed memory
    /// and metadata values come back as strings, numbers and booleans rather than <c>JsonElement</c>, and a
    /// reloaded transition replays the same as the live one. Polymorphic members (e.g. <c>AgentEvent</c>)
    /// are still not supported.
    /// </remarks>
    public sealed class FileTransitionStore : ITransitionStore
    {
        private readonly JsonLinesFile<Transition> _file;

        /// <summary>Creates the store, ensuring the directory and file exist.</summary>
        /// <param name="filePath">Path of the JSONL file.</param>
        public FileTransitionStore(string filePath)
        {
            _file = new JsonLinesFile<Transition>(filePath);
        }

        /// <summary>Full path of the backing file.</summary>
        public string FilePath => _file.FilePath;

        /// <inheritdoc />
        public void Append(Transition transition)
        {
            _file.Append(transition);
        }

        /// <inheritdoc />
        /// <remarks>Reads and deserialises the whole file on every call.</remarks>
        public IReadOnlyList<Transition> GetAll()
        {
            return _file.ReadAll();
        }
    }
}

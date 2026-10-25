using System.Text;
using System.Text.Json;
using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Transition;

namespace InvariantAgent.Storage
{
    /// <summary>
    /// Durable <see cref="ITransitionStore"/> that appends each transition as one JSON line (JSONL).
    /// </summary>
    /// <remarks>
    /// Thread-safe within one process. Polymorphic members (e.g. <c>AgentEvent</c>) and
    /// <c>object</c>-typed metadata may not round-trip exactly through System.Text.Json.
    /// </remarks>
    public sealed class FileTransitionStore : ITransitionStore
    {
        private readonly string _filePath;
        private readonly object _lock = new();

        private static readonly JsonSerializerOptions Options =
            new JsonSerializerOptions
            {
                WriteIndented = false,
                IncludeFields = true
            };

        /// <summary>Creates the store, ensuring the directory and file exist.</summary>
        /// <param name="filePath">Path of the JSONL file.</param>
        public FileTransitionStore(string filePath)
        {
            _filePath = filePath;

            var directory = Path.GetDirectoryName(_filePath);

            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (!File.Exists(_filePath))
            {
                using var _ = File.Create(_filePath);
            }
        }

        /// <inheritdoc />
        public void Append(Transition transition)
        {
            var json = JsonSerializer.Serialize(
                transition,
                Options);

            lock (_lock)
            {
                File.AppendAllText(
                    _filePath,
                    json + Environment.NewLine,
                    Encoding.UTF8);
            }
        }

        /// <inheritdoc />
        /// <remarks>Reads and deserialises the whole file on every call.</remarks>
        public IReadOnlyList<Transition> GetAll()
        {
            lock (_lock)
            {
                var result = new List<Transition>();

                foreach (var line in File.ReadLines(_filePath))
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    var transition =
                        JsonSerializer.Deserialize<Transition>(
                            line,
                            Options);

                    if (transition != null)
                    {
                        result.Add(transition);
                    }
                }

                return result;
            }
        }
    }
}
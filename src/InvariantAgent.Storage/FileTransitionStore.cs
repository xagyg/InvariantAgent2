using System.Text;
using System.Text.Json;
using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Transition;

namespace InvariantAgent.Storage
{
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
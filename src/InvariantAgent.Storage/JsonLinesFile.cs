using System.Text;
using System.Text.Json;
using InvariantAgent.Core.Serialization;

namespace InvariantAgent.Storage
{
    /// <summary>
    /// Append-only JSON Lines file shared by the durable stores. One record per line, serialized with
    /// <see cref="InvariantAgentJson.Options"/> so records read back match what was written.
    /// </summary>
    /// <typeparam name="T">Record type.</typeparam>
    internal sealed class JsonLinesFile<T> where T : class
    {
        private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

        private readonly string _filePath;
        private readonly object _lock = new();

        /// <summary>Opens (creating if needed) the file and its directory.</summary>
        /// <param name="filePath">Path of the JSONL file.</param>
        public JsonLinesFile(string filePath)
        {
            _filePath = Path.GetFullPath(filePath);

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

        /// <summary>Full path of the backing file.</summary>
        public string FilePath => _filePath;

        /// <summary>Appends one record as a single line.</summary>
        public void Append(T record)
        {
            var json = JsonSerializer.Serialize(record, InvariantAgentJson.Options);

            lock (_lock)
            {
                File.AppendAllText(_filePath, json + Environment.NewLine, Utf8NoBom);
            }
        }

        /// <summary>Reads every record in file order.</summary>
        /// <remarks>
        /// An unreadable <em>final</em> line is skipped: it is what a crash part-way through an append leaves
        /// behind. An unreadable line anywhere else means the history itself is damaged, so it throws rather
        /// than silently dropping governance records.
        /// </remarks>
        /// <exception cref="InvalidDataException">A line other than the last cannot be parsed.</exception>
        public IReadOnlyList<T> ReadAll()
        {
            lock (_lock)
            {
                var lines = File.ReadAllLines(_filePath);
                var lastContentLine = Array.FindLastIndex(lines, l => !string.IsNullOrWhiteSpace(l));
                var result = new List<T>();

                for (var index = 0; index < lines.Length; index++)
                {
                    var line = lines[index];

                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    try
                    {
                        var record = JsonSerializer.Deserialize<T>(line, InvariantAgentJson.Options);

                        if (record != null)
                        {
                            result.Add(record);
                        }
                    }
                    catch (JsonException) when (index == lastContentLine)
                    {
                        // Torn write from an interrupted append; the record was never fully stored.
                    }
                    catch (JsonException ex)
                    {
                        throw new InvalidDataException(
                            $"Unreadable record on line {index + 1} of '{_filePath}': {ex.Message}", ex);
                    }
                }

                return result;
            }
        }
    }
}

using System.Text.Json;

namespace InvariantAgent.Core.Extensions
{
    public static class CloneExtensions
    {
        private static readonly JsonSerializerOptions Options =
            new JsonSerializerOptions
            {
                IncludeFields = true,
                WriteIndented = false
            };

        public static T Clone<T>(this T value)
        {
            if (value == null)
            {
                return default!;
            }

            var json = JsonSerializer.Serialize(value, Options);

            return JsonSerializer.Deserialize<T>(json, Options)!;
        }
    }
}
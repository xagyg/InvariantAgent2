using System;
using System.Collections.Generic;
using System.Linq;

namespace InvariantAgent.Core.Model
{
    public class CapabilityResult
    {
        public bool Success { get; set; }

        public string Capability { get; set; } = "";

        public Data.CapabilityData Data { get; set; }

        public string Error { get; set; }

        public DateTime Timestamp { get; set; }

        public static CapabilityResult Ok(string capability, Data.CapabilityData data)
        {
            return new CapabilityResult
            {
                Capability = capability,
                Success = true,
                Data = data,
                Timestamp = DateTime.UtcNow
            };
        }

        public static CapabilityResult Fail(string capability, string error)
        {
            return new CapabilityResult
            {
                Capability = capability,
                Success = false,
                Error = error,
                Timestamp = DateTime.UtcNow
            };
        }

        public override string ToString()
        {
            if (!Success)
                return $"Capability={Capability}, Error={Error}";

            return $"Capability={Capability}, Data={Format(Data)}";
        }

        private static string Format(object? value)
        {
            if (value is null)
                return "null";

            if (value is string s)
                return s;

            if (value is System.Collections.IEnumerable enumerable && value is not string)
            {
                var items = enumerable
                    .Cast<object?>()
                    .Select(Format);

                return $"[{string.Join(", ", items)}]";
            }

            // 🔥 handle complex objects (anonymous types too)
            var type = value.GetType();
            if (type.IsPrimitive || type.IsEnum)
                return value.ToString()!;

            var props = type.GetProperties();

            if (props.Length == 0)
                return value.ToString()!;

            var parts = new List<string>();

            foreach (var prop in props)
            {
                var propValue = prop.GetValue(value);
                parts.Add($"{prop.Name}={Format(propValue)}");
            }

            return $"{{ {string.Join(", ", parts)} }}";
        }
    }
}

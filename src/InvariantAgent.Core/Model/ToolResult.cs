using System;
using System.Collections.Generic;
using System.Linq;

namespace InvariantAgent.Core.Model
{
    public class ToolResult
    {
        public bool Success { get; set; }

        public string Tool { get; set; } = "";

        public ToolData.ToolData Data { get; set; }

        public string Error { get; set; }

        public DateTime Timestamp { get; set; }

        public static ToolResult Ok(string tool, ToolData.ToolData data)
        {
            return new ToolResult
            {
                Tool = tool,
                Success = true,
                Data = data,
                Timestamp = DateTime.UtcNow
            };
        }

        public static ToolResult Fail(string tool, string error)
        {
            return new ToolResult
            {
                Tool = tool,
                Success = false,
                Error = error,
                Timestamp = DateTime.UtcNow
            };
        }

        public override string ToString()
        {
            if (!Success)
                return $"Tool={Tool}, Error={Error}";

            return $"Tool={Tool}, Data={Format(Data)}";
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

using System;
using System.Collections.Generic;
using System.Linq;
using InvariantAgent.Core.Model.Data;
using InvariantAgent.Core.Model.SelfModification;

namespace InvariantAgent.Core.Model.Capability
{
    public sealed class CapabilityResult
    {
        public bool Success { get; init; }

        public string Capability { get; init; } = "";

        public CapabilityData Data { get; init; }

        public string Error { get; init; } = "";

        public DateTime Timestamp { get; init; } = DateTime.UtcNow;

        public SelfModificationRequest ProposedModification { get; set; }

        public static CapabilityResult Ok(
            string capability,
            CapabilityData data,
            SelfModificationRequest proposed = null)
        {
            return new CapabilityResult
            {
                Capability = capability,
                Success = true,
                Data = data,
                ProposedModification = proposed
            };
        }

        public static CapabilityResult Fail(
            string capability,
            string error)
        {
            return new CapabilityResult
            {
                Capability = capability,
                Success = false,
                Error = error
            };
        }

        public override string ToString()
        {
            if (!Success)
                return $"Capability={Capability}, Error={Error}";

            return $"Capability={Capability}, Data={Format(Data)}";
        }

        private static string Format(object value)
        {
            if (value is null)
                return "null";

            if (value is string s)
                return s;

            if (value is System.Collections.IEnumerable enumerable
                && value is not string)
            {
                var items = enumerable
                    .Cast<object>()
                    .Select(Format);

                return $"[{string.Join(", ", items)}]";
            }

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
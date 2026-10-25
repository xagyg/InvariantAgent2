using System;
using System.Collections.Generic;
using System.Linq;
using InvariantAgent.Core.Model.Data;
using InvariantAgent.Core.Model.SelfModification;

namespace InvariantAgent.Core.Model.Capability
{
    /// <summary>
    /// The result returned by a capability, optionally carrying a proposed self-modification.
    /// </summary>
    /// <remarks>
    /// Capabilities cannot modify runtime state directly. Instead they attach a
    /// <see cref="SelfModificationRequest"/> to <see cref="ProposedModification"/>, which the runtime
    /// evaluates against self-modification invariants before the reducer applies it.
    /// </remarks>
    public sealed class CapabilityResult
    {
        /// <summary><c>true</c> if the capability executed successfully.</summary>
        public bool Success { get; init; }

        /// <summary>Name of the capability that produced the result.</summary>
        public string Capability { get; init; } = "";

        /// <summary>Typed payload produced on success.</summary>
        public CapabilityData Data { get; init; }

        /// <summary>Failure reason when <see cref="Success"/> is <c>false</c>.</summary>
        public string Error { get; init; } = "";

        /// <summary>UTC time the result was created.</summary>
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;

        /// <summary>A proposed change to agent state, subject to governance. <c>null</c> if none.</summary>
        public SelfModificationRequest ProposedModification { get; set; }

        /// <summary>Creates a successful result.</summary>
        /// <param name="capability">Capability name.</param>
        /// <param name="data">Result payload.</param>
        /// <param name="proposed">Optional proposed self-modification.</param>
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

        /// <summary>Creates a failed result.</summary>
        /// <param name="capability">Capability name.</param>
        /// <param name="error">Failure reason.</param>
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

        /// <summary>Diagnostic string showing either the data or the error.</summary>
        public override string ToString()
        {
            if (!Success)
                return $"Capability={Capability}, Error={Error}";

            return $"Capability={Capability}, Data={Format(Data)}";
        }

        // Recursively formats strings, collections, primitives and simple objects (via public properties).
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
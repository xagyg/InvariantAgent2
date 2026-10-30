using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace InvariantAgent.Core.Serialization
{
    /// <summary>
    /// Shared JSON settings for persisting and cloning transitions, states and baselines.
    /// </summary>
    /// <remarks>
    /// Every persistence path (transition store, baseline store, <c>Clone()</c>) must use these options so a
    /// record read back from disk is equivalent to the one that was written. Without
    /// <see cref="InferredObjectConverter"/>, values typed as <c>object</c> (e.g. <c>AgentState.Memory</c>
    /// and <c>TransitionEvent.Metadata</c>) deserialize as <see cref="JsonElement"/>, which makes replayed
    /// transitions differ from live ones.
    /// </remarks>
    public static class InvariantAgentJson
    {
        /// <summary>Options used for storage and deep cloning.</summary>
        public static readonly JsonSerializerOptions Options = Create();

        private static JsonSerializerOptions Create()
        {
            var options = new JsonSerializerOptions
            {
                IncludeFields = true,
                WriteIndented = false
            };

            options.Converters.Add(new InferredObjectConverter());

            return options;
        }
    }

    /// <summary>
    /// Reads JSON primitives held in <c>object</c>-typed slots back as CLR primitives
    /// (string, bool, int/long/double, null) instead of <see cref="JsonElement"/>.
    /// Arrays and objects are kept as a detached <see cref="JsonElement"/> because their original type is unknown.
    /// </summary>
    public sealed class InferredObjectConverter : JsonConverter<object>
    {
        /// <inheritdoc />
        public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.True:
                    return true;

                case JsonTokenType.False:
                    return false;

                case JsonTokenType.String:
                    return reader.GetString();

                case JsonTokenType.Number:
                    if (reader.TryGetInt32(out var i))
                    {
                        return i;
                    }

                    if (reader.TryGetInt64(out var l))
                    {
                        return l;
                    }

                    return reader.GetDouble();

                case JsonTokenType.Null:
                    return null;

                default:
                    using (var document = JsonDocument.ParseValue(ref reader))
                    {
                        return document.RootElement.Clone();
                    }
            }
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            var runtimeType = value.GetType();

            // A bare object has no members; serializing it as object would recurse into this converter.
            if (runtimeType == typeof(object))
            {
                writer.WriteStartObject();
                writer.WriteEndObject();
                return;
            }

            JsonSerializer.Serialize(writer, value, runtimeType, options);
        }
    }
}

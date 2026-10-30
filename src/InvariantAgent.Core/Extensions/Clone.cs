using System.Text.Json;
using InvariantAgent.Core.Serialization;

namespace InvariantAgent.Core.Extensions
{
    /// <summary>
    /// Deep-clone helper based on a JSON serialisation round-trip.
    /// </summary>
    /// <remarks>
    /// Used, for example, by replay validation so invariants can be re-evaluated on a copy without
    /// mutating the stored transition. Uses <see cref="InvariantAgentJson.Options"/> so a clone matches what
    /// the file store would read back. Polymorphic or non-serialisable members may be lost.
    /// </remarks>
    public static class CloneExtensions
    {
        /// <summary>Returns a deep copy of <paramref name="value"/> (or <c>default</c> for <c>null</c>).</summary>
        /// <typeparam name="T">The type to clone.</typeparam>
        /// <param name="value">The value to copy.</param>
        /// <returns>A new instance with the same serialisable content.</returns>
        public static T Clone<T>(this T value)
        {
            if (value == null)
            {
                return default!;
            }

            var json = JsonSerializer.Serialize(value, InvariantAgentJson.Options);

            return JsonSerializer.Deserialize<T>(json, InvariantAgentJson.Options)!;
        }
    }
}
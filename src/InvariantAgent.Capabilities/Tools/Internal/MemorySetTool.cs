using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Capability;
using InvariantAgent.Core.Model.Data;
using InvariantAgent.Core.Model.SelfModification;

namespace InvariantAgent.Capabilities.Tools.Internal
{
    /// <summary>
    /// <c>memory-set key=value</c>: proposes a governed memory update.
    /// </summary>
    /// <remarks>
    /// The tool does not change memory itself. It returns a <c>SelfModificationRequest</c>
    /// which must pass self-modification invariants (e.g. <c>AllowedMemoryKeyInvariant</c>) before the
    /// reducer applies it.
    /// </remarks>
    public sealed class MemorySetTool
        : ICapability
    {
        /// <inheritdoc />
        public string Name => "memory-set";

        /// <inheritdoc />
        public CapabilityResult Execute(CapabilityRequest request)
        {
            var parts = request.Input.Split(
                '=',
                2,
                StringSplitOptions.TrimEntries);

            if (parts.Length != 2)
            {
                return CapabilityResult.Fail(
                    Name,
                    "Expected format: key=value");
            }

            var modification =
                new SelfModificationRequest
                {
                    Target = "memory",
                    Operation = "set",
                    Key = parts[0],
                    Value = parts[1]
                };

            return CapabilityResult.Ok(Name,
                    new TextData
                    {
                        Value = $"Proposed memory update: {parts[0]}"
                    },
                    modification
                );
        }
    }
}
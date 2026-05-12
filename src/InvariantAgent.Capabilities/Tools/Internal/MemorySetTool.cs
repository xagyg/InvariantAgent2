using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Capability;
using InvariantAgent.Core.Model.Data;
using InvariantAgent.Core.Model.SelfModification;

namespace InvariantAgent.Capabilities.Tools.Internal
{
    public sealed class MemorySetTool
        : ICapability
    {
        public string Name => "memory-set";

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
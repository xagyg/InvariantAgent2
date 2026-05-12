using System.Text;
using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Capability;
using InvariantAgent.Core.Model.Data;

namespace InvariantAgent.Capabilities.Tools.Internal
{
    public sealed class MemoryShowTool : ICapability
    {
        public string Name => "memory-show";

        private readonly ITransitionStore _store;

        public MemoryShowTool(ITransitionStore store)
        {
            _store = store;
        }

        public CapabilityResult Execute(CapabilityRequest request)
        {
            var latestState = _store.GetAll().LastOrDefault(t => t.After != null)?.After;

            if (latestState == null || latestState.Memory.Count == 0)
            {
                return CapabilityResult.Ok(
                    Name,
                    new TextData
                    {
                        Value = "Memory is empty."
                    });
            }

            var sb = new StringBuilder();

            sb.AppendLine("\n==== MEMORY ====");

            foreach (var item in latestState.Memory)
            {
                sb.AppendLine($"{item.Key}={item.Value}");
            }

            sb.Append("==== END MEMORY ====");

            return CapabilityResult.Ok(
                Name,
                new TextData
                {
                    Value = sb.ToString()
                });
        }
    }
}
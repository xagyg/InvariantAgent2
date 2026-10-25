using System.Text;
using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Capability;
using InvariantAgent.Core.Model.Data;
using InvariantAgent.Core.Model.Transition;

namespace InvariantAgent.Capabilities.Tools.Internal
{
    /// <summary><c>memory-show</c>: lists memory from the latest committed state.</summary>
    /// <remarks>
    /// Only completed transitions are considered. A transition rejected at the Reduction checkpoint still has
    /// a proposed <c>After</c> state, but that state was never committed and is ignored here.
    /// </remarks>
    public sealed class MemoryShowTool : ICapability
    {
        /// <inheritdoc />
        public string Name => "memory-show";

        private readonly ITransitionStore _store;

        /// <summary>Creates the tool.</summary>
        /// <param name="store">Transition history used to find the latest committed state.</param>
        public MemoryShowTool(ITransitionStore store)
        {
            _store = store;
        }

        /// <inheritdoc />
        public CapabilityResult Execute(CapabilityRequest request)
        {
            var latestState = _store.GetAll()
                .LastOrDefault(t => t.Status == TransitionStatus.Completed && t.After != null)
                ?.After;

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
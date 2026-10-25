using System.Text;
using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Capability;
using InvariantAgent.Core.Model.Data;

namespace InvariantAgent.Capabilities.Tools.Internal
{
    /// <summary><c>memory-show</c>: lists memory from the most recent transition that produced a resulting state.</summary>
    /// <remarks>
    /// "Most recent resulting state" is taken as the last transition with a non-null <c>After</c>. A transition
    /// rejected at the Reduction checkpoint also has <c>After</c> set, so it can be picked up here even though
    /// its state was never committed.
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
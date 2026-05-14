using System.Text;
using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Capability;
using InvariantAgent.Core.Model.Data;
using InvariantAgent.Core.Rendering;

namespace InvariantAgent.Capabilities.Tools.Internal
{
    public sealed class ReplayTool : ICapability
    {
        private readonly ITransitionStore _store;

        public string Name => "replay";

        public IReadOnlyCollection<string> Aliases => new[]
        {
            "history",
            "audit",
            "trace"
        };

        public ReplayTool(ITransitionStore store)
        {
            _store = store;
        }

        public CapabilityResult Execute(CapabilityRequest request)
        {
            var transitions = _store.GetAll();

            if (transitions.Count == 0)
            {
                return CapabilityResult.Ok(
                    Name,
                    new TextData
                    {
                        Value = "==== REPLAY START ====\nNo transitions recorded.\n==== REPLAY END ===="
                    });
            }

            var count = transitions.Count;

            if (int.TryParse(request.Input, out var requested))
            {
                count = Math.Min(requested, transitions.Count);
            }

            var verbose = request.Input.Contains("verbose", StringComparison.OrdinalIgnoreCase);

            var result = verbose 
                ? TransitionFormatter.FormatReplayVerbose(transitions.TakeLast(count))
                : TransitionFormatter.FormatReplay(transitions.TakeLast(count));

            return CapabilityResult.Ok(
                Name,
                new TextData
                {
                    Value = result
                });
        }
    }
}
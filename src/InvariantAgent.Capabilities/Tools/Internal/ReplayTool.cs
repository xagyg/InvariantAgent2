using System.Text;
using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Extensions;
using InvariantAgent.Core.Model.Capability;
using InvariantAgent.Core.Model.Data;
using InvariantAgent.Core.Model.Transition;
using InvariantAgent.Core.Rendering;
using InvariantAgent.Core.Replay;
using Microsoft.Extensions.DependencyInjection;

namespace InvariantAgent.Capabilities.Tools.Internal
{
    public sealed class ReplayTool : ICapability
    {
        private readonly ITransitionStore _store;
        private readonly IServiceProvider _services;

        public string Name => "replay";

        public IReadOnlyCollection<string> Aliases => new[]
        {
            "history",
            "audit",
            "trace"
        };

        public ReplayTool(ITransitionStore store, IServiceProvider services)
        {
            _store = store;
            _services = services;
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

            var input = request.Input?.Trim() ?? "";

            var validate = input.Contains("validate", StringComparison.OrdinalIgnoreCase);

            var verbose = input.Contains("verbose", StringComparison.OrdinalIgnoreCase);

            var count = ParseCount(input, transitions.Count);

            var selectedTransitions = transitions.TakeLast(count).ToList();

            var result = verbose
                ? TransitionFormatter.FormatReplayVerbose(selectedTransitions)
                : TransitionFormatter.FormatReplay(selectedTransitions);

            if (validate)
            {
                result += FormatValidation(selectedTransitions);
            }

            return CapabilityResult.Ok(
                Name,
                new TextData
                {
                    Value = result
                });
        }

        private string FormatValidation(IReadOnlyList<Transition> transitions)
        {
            var sb = new StringBuilder();

            sb.AppendLine();
            sb.AppendLine("==== REPLAY VALIDATION START ====");

            foreach (var transition in transitions)
            {                 
                var context = new TransitionContext
                {
                    // do not use real transition as this is for reporting only
                    Transition = transition.Clone()
                };

                var validator = _services.GetRequiredService<ReplayValidator>();

                var result = validator.Validate(context);

                sb.AppendLine(
                    result.Passed
                        ? $"[{transition.Id} {transition.Status}] Passed"
                        : $"[{transition.Id} {transition.Status}] Drift={result.DriftType}, Reason={result.Reason}");
            }

            sb.AppendLine("==== REPLAY VALIDATION END ====");

            return sb.ToString();
        }

        private static int ParseCount(string input, int defaultCount)
        {
            var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                if (int.TryParse(part, out var count))
                {
                    return Math.Min(count, defaultCount);
                }
            }

            return defaultCount;
        }
    }
}
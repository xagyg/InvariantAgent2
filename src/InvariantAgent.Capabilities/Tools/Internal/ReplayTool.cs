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
    /// <summary>
    /// <c>replay [N] [verbose] [validate]</c> (aliases: history, audit, trace): replays recorded transitions.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><c>N</c> limits output to the last N transitions.</item>
    /// <item><c>verbose</c> includes each transition's full event trail.</item>
    /// <item><c>validate</c> re-evaluates invariants on a copy of each transition and reports governance drift.</item>
    /// </list>
    /// </remarks>
    public sealed class ReplayTool : ICapability
    {
        private readonly ITransitionStore _store;
        private readonly IServiceProvider _services;

        /// <inheritdoc />
        public string Name => "replay";

        /// <inheritdoc />
        public IReadOnlyCollection<string> Aliases => new[]
        {
            "history",
            "audit",
            "trace"
        };

        /// <summary>Creates the tool.</summary>
        /// <param name="store">Transition history source.</param>
        /// <param name="services">
        /// Service provider used to resolve <c>ReplayValidator</c> lazily (avoids a construction-time
        /// dependency cycle through the invariant evaluator and capability registry).
        /// </param>
        public ReplayTool(ITransitionStore store, IServiceProvider services)
        {
            _store = store;
            _services = services;
        }

        /// <inheritdoc />
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
                result += FormatValidation(selectedTransitions, verbose);
            }

            return CapabilityResult.Ok(
                Name,
                new TextData
                {
                    Value = result
                });
        }

        // Runs replay validation for each transition and formats the comparison.
        // Details are shown when verbose, or always for mismatches.
        private string FormatValidation(IReadOnlyList<Transition> transitions, bool verbose)
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

                var showDetails = verbose || !result.Passed;

                if (showDetails && result.Comparison != null)
                {
                    var comparison = result.Comparison;

                    sb.AppendLine($"  OriginalStatus={comparison.OriginalStatus}");

                    sb.AppendLine($"  ReplayStatus={comparison.ReplayStatus}");

                    sb.AppendLine($"  OriginalPhase={comparison.OriginalPhase}");

                    sb.AppendLine($"  ReplayPhase={comparison.ReplayPhase}");

                    if (comparison.OriginalViolations.Count > 0)
                    {
                        sb.AppendLine($"  OriginalViolations={comparison.OriginalViolations.Count}");

                        foreach (var violation in comparison.OriginalViolations)
                        {
                            sb.AppendLine($"    - {violation}");
                        }
                    }

                    if (comparison.ReplayViolations.Count > 0)
                    {
                        sb.AppendLine($"  ReplayViolations={comparison.ReplayViolations.Count}");

                        foreach (var violation in comparison.ReplayViolations)
                        {
                            sb.AppendLine($"    - {violation}");
                        }
                    }
                }

                sb.AppendLine(result.Passed
                        ? $"[{transition.Id} {transition.Status}] Passed"
                        : $"[{transition.Id} {transition.Status}] Drift={result.DriftType}, Reason={result.Reason}");
            }

            sb.AppendLine("==== REPLAY VALIDATION END ====");

            return sb.ToString();
        }

        // Returns the first integer token in the input (capped at the history length), or all transitions.
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
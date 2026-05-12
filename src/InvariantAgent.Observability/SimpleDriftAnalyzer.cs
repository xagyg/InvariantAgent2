using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Drift;
using InvariantAgent.Core.Model.Transition;

namespace InvariantAgent.Observability
{
    public sealed class SimpleDriftAnalyzer : IDriftAnalyzer
    {
        public DriftReport Analyze(IReadOnlyList<Transition> transitions)
        {
            var capabilityUsage = transitions
                .Where(t => t.ProposedAction != null)
                .GroupBy(t => t.ProposedAction!.Capability)
                .ToDictionary(g => g.Key, g => g.Count());

            var invariantFailures = transitions
                .SelectMany(t => t.Events)
                .Where(e => e.Stage == "Invariant" && e.Message.Contains("Failed"))
                .Select(e => ExtractInvariantName(e.Message))
                .GroupBy(name => name)
                .ToDictionary(g => g.Key, g => g.Count());

            return new DriftReport
            {
                TransitionCount = transitions.Count,

                RejectedTransitions = transitions.Count(t => t.Status == TransitionStatus.Rejected),

                CapabilityUsage = capabilityUsage,

                InvariantFailures = invariantFailures
            };
        }

        private static string ExtractInvariantName(
            string message)
        {
            var index = message.IndexOf(':');

            if (index <= 0) return "Unknown";

            return message.Substring(0, index);
        }
    }
}
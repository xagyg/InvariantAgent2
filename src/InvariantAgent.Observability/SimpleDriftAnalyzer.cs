using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Drift;
using InvariantAgent.Core.Model.Control;
using InvariantAgent.Core.Model.Drift;
using InvariantAgent.Core.Model.Transition;

namespace InvariantAgent.Observability
{
    public sealed class SimpleDriftAnalyzer : IDriftAnalyzer
    {
        private readonly DriftTracker _driftTracker;
        private readonly BehaviouralDriftDetector _behaviouralDriftDetector;

        public SimpleDriftAnalyzer(
            DriftTracker driftTracker,
            BehaviouralDriftDetector behaviouralDriftDetector)
        {
            _driftTracker = driftTracker;
            _behaviouralDriftDetector = behaviouralDriftDetector;
        }

        public DriftReport Analyze(IReadOnlyList<Transition> transitions)
        {
            var capabilityUsage = transitions
                .Where(t => t.ProposedAction != null)
                .GroupBy(t => t.ProposedAction!.Capability)
                .ToDictionary(g => g.Key, g => g.Count());

            var invariantFailures = transitions
                .SelectMany(t => t.Events)
                .Where(e => (e.Stage == TransitionEventStage.Invariant) && e.Message.Contains("Failed"))
                .Select(e => ExtractInvariantName(e.Message))
                .GroupBy(name => name)
                .ToDictionary(g => g.Key, g => g.Count());

            var records = _driftTracker.Records
                .Concat(_behaviouralDriftDetector.Detect(transitions))
                .OrderBy(r => r.TimestampUtc)
                .ToList();

            var driftCounts = records.GroupBy(r => r.Type).ToDictionary(g => g.Key, g => g.Count());

            var totalDriftScore = records.Sum(r => r.Score);

            var highestSeverity = records.Count == 0 ? InvariantSeverity.Info : records.Max(r => r.Severity);

            return new DriftReport
            {
                TransitionCount = transitions.Count,

                RejectedTransitions = transitions.Count(t => t.Status == TransitionStatus.Rejected),

                CapabilityUsage = capabilityUsage,

                InvariantFailures = invariantFailures,

                DriftCounts = driftCounts,

                RecentDrift = records.TakeLast(10).ToList(),

                TotalDriftScore = totalDriftScore,

                HighestDriftSeverity = highestSeverity
            };        
        }

        private static string ExtractInvariantName(string message)
        {
            var index = message.IndexOf(':');

            if (index <= 0) return "Unknown";

            return message.Substring(0, index);
        }
    }
}

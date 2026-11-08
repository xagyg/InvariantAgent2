using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Drift;
using InvariantAgent.Core.Model.Control;
using InvariantAgent.Core.Model.Drift;
using InvariantAgent.Core.Model.Transition;

namespace InvariantAgent.Observability
{
    /// <summary>
    /// Default <see cref="IDriftAnalyzer"/>. Combines runtime drift records (e.g. governance drift from
    /// replay validation) with behavioural drift detected from history, and attaches a stability assessment.
    /// </summary>
    public sealed class SimpleDriftAnalyzer : IDriftAnalyzer
    {
        private readonly DriftTracker _driftTracker;
        private readonly BehaviouralDriftDetector _behaviouralDriftDetector;
        private readonly IStabilityEvaluator _stabilityEvaluator;

        /// <summary>Creates the analyzer.</summary>
        /// <param name="driftTracker">Source of drift records raised at runtime.</param>
        /// <param name="behaviouralDriftDetector">Detector for transition-local behavioural drift.</param>
        /// <param name="stabilityEvaluator">Evaluator producing the stability assessment.</param>
        public SimpleDriftAnalyzer(
            DriftTracker driftTracker,
            BehaviouralDriftDetector behaviouralDriftDetector,
            IStabilityEvaluator stabilityEvaluator)
        {
            _driftTracker = driftTracker;
            _behaviouralDriftDetector = behaviouralDriftDetector;
            _stabilityEvaluator = stabilityEvaluator;
        }

        /// <inheritdoc />
        public DriftReport Analyze(IReadOnlyList<Transition> transitions)
        {
            var capabilityUsage = transitions
                .Where(t => t.ProposedAction != null)
                .GroupBy(t => t.ProposedAction!.Capability)
                .ToDictionary(g => g.Key, g => g.Count());

            var invariantFailures = transitions
                .SelectMany(t => t.Events)
                .Where(IsFailedInvariantEvent)
                .Select(e => ExtractInvariantName(e.Message) + " [" + ExtractInvariantCategory(e) +"]")
                .GroupBy(name => name)
                .ToDictionary(g => g.Key, g => g.Count());

            var invariantFailuresByCategory = transitions
                .SelectMany(t => t.Events)
                .Where(IsFailedInvariantEvent)
                .Select(ExtractInvariantCategory)
                .GroupBy(category => category)
                .ToDictionary(g => g.Key, g => g.Count());

            // Merge tracked (runtime) drift with behavioural drift recomputed from history.
            var records = _driftTracker.Records
                .Concat(_behaviouralDriftDetector.Detect(transitions))
                .OrderBy(r => r.TimestampUtc)
                .ToList();

            var driftCounts = records.GroupBy(r => r.Type).ToDictionary(g => g.Key, g => g.Count());

            var totalDriftScore = records.Sum(r => r.Score);

            var highestSeverity = records.Count == 0 ? InvariantSeverity.Info : records.Max(r => r.Severity);

            // HIG-C (IBAAM 3 revised, Section 9): split behavioural drift by whether governance explains it.
            var byId = transitions
                .GroupBy(t => t.Id.ToString())
                .ToDictionary(g => g.Key, g => g.Last());
            var behavioural = records.Where(r => r.Type == DriftType.BehaviouralDrift).ToList();
            var attributed = behavioural
                .Where(r => byId.TryGetValue(r.TransitionId, out var t) && PrescribesChange(t))
                .ToList();
            var unattributed = behavioural.Except(attributed).ToList();
            var behaviouralScore = behavioural.Sum(r => r.Score);

            return new DriftReport
            {
                TransitionCount = transitions.Count,

                RejectedTransitions = transitions.Count(t => t.Status == TransitionStatus.Rejected),

                CapabilityUsage = capabilityUsage,

                InvariantFailures = invariantFailures,

                InvariantFailuresByCategory = invariantFailuresByCategory,

                DriftCounts = driftCounts,

                RecentDrift = records.TakeLast(10).ToList(),

                TotalDriftScore = totalDriftScore,

                HighestDriftSeverity = highestSeverity,

                ConflictTransitions = transitions.Count(t => t.Conflicts != null && t.Conflicts.Count > 0),

                EscalatedTransitions = transitions.Count(t => t.GovernanceEscalated),

                AttributedDrift = Attribution(attributed, behaviouralScore),

                UnattributedDrift = Attribution(unattributed, behaviouralScore),

                MissingExpectedChange = transitions.SelectMany(MissingExpectedChange).ToList(),

                Stability = _stabilityEvaluator.Evaluate(transitions)
            };        
        }

        // A transition whose conflict record prescribes different behaviour from simply carrying out the proposal.
        private static bool PrescribesChange(Transition transition)
        {
            return transition.Conflicts != null && transition.Conflicts.Any(c => c.ChangesBehaviour);
        }

        private static DriftAttribution Attribution(IReadOnlyList<DriftRecord> subset, int total)
        {
            var score = subset.Sum(r => r.Score);

            return new DriftAttribution
            {
                Events = subset.Count,
                Score = score,
                Share = total == 0 ? 0 : (double)score / total
            };
        }

        /// <summary>
        /// Conflict records whose prescribed outcome did not take effect on the transition (Section 9, "missing expected
        /// change"): a decline on a transition that still completed, a selected alternative that was not the capability
        /// executed, or an overridden invariant that still appears among the blocking invariants.
        /// </summary>
        private static IEnumerable<string> MissingExpectedChange(Transition transition)
        {
            if (transition.Conflicts == null)
            {
                yield break;
            }

            foreach (var conflict in transition.Conflicts)
            {
                var id = transition.Id.ToString("N")[..8];

                if (conflict.Selected == GovernanceCandidate.DeclineId)
                {
                    if (transition.Status == TransitionStatus.Completed)
                    {
                        yield return $"{id} {conflict.Scope}: declined, but the transition completed.";
                    }

                    continue;
                }

                if (conflict.Selected != GovernanceCandidate.ProposedId &&
                    transition.Outcome != null &&
                    !string.Equals(transition.Outcome.Capability, conflict.SelectedCapability, StringComparison.OrdinalIgnoreCase))
                {
                    yield return $"{id} {conflict.Scope}: selected {conflict.Selected} ({conflict.SelectedCapability}), " +
                                 $"but '{transition.Outcome.Capability}' executed.";
                }

                var stillBlocking = conflict.Overridden
                    .Intersect(transition.BlockingInvariants ?? new List<string>())
                    .ToList();

                if (stillBlocking.Count > 0)
                {
                    yield return $"{id} {conflict.Scope}: overrode {string.Join(", ", stillBlocking)}, " +
                                 "but it still blocked the transition.";
                }
            }
        }

        // Invariant events are formatted "Name: Passed|Failed reason"; take the text before the colon.
        private static string ExtractInvariantName(string message)
        {
            var index = message.IndexOf(':');

            if (index <= 0) return "Unknown";

            return message.Substring(0, index);
        }

        // Prefers structured "Passed" metadata; falls back to message text for older events.
        private static bool IsFailedInvariantEvent(TransitionEvent e)
        {
            if (e.Stage != TransitionEventStage.Invariant)
            {
                return false;
            }

            if (e.Metadata.TryGetValue("Passed", out var passed))
            {
                return passed is bool value && !value;
            }

            return e.Message.Contains("Failed");
        }

        // Reads the "Category" metadata; defaults to Integrity when missing.
        private static InvariantCategory ExtractInvariantCategory(TransitionEvent e)
        {
            if (e.Metadata.TryGetValue("Category", out var value) &&
                Enum.TryParse<InvariantCategory>(
                    value?.ToString(),
                    ignoreCase: true,
                    out var category))
            {
                return category;
            }

            return InvariantCategory.Integrity;
        }
    }
}

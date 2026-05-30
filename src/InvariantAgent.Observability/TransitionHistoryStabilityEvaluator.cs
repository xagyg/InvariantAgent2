using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Drift;
using InvariantAgent.Core.Model.Agent;
using InvariantAgent.Core.Model.Stability;
using InvariantAgent.Core.Model.Transition;

namespace InvariantAgent.Observability
{
    public sealed class TransitionHistoryStabilityEvaluator : IStabilityEvaluator
    {
        private static readonly HashSet<string> VolatileMemoryKeys =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "lastOutcome",
                "last_action"
            };

        private readonly BehaviouralDriftDetector _driftDetector;

        public TransitionHistoryStabilityEvaluator(BehaviouralDriftDetector driftDetector)
        {
            _driftDetector = driftDetector;
        }

        public StabilityAssessment Evaluate(IReadOnlyList<Transition> transitions)
        {
            var completed = transitions
                .Where(t => t.Status == TransitionStatus.Completed && t.After != null)
                .ToList();

            if (completed.Count == 0)
            {
                return new StabilityAssessment
                {
                    Current = new StabilityVector(),
                    Confidence = MeasurementConfidence.Low,
                    Region = StabilityRegion.Stable,
                    Recommendations = new[]
                    {
                        "Collect completed transitions before interpreting stability scores."
                    }
                };
            }

            var currentWindow = completed.TakeLast(Math.Min(BehaviouralDriftDetector.DefaultHorizon, completed.Count)).ToList();
            var priorWindow = completed
                .Take(Math.Max(0, completed.Count - currentWindow.Count))
                .TakeLast(Math.Min(BehaviouralDriftDetector.DefaultHorizon, Math.Max(0, completed.Count - currentWindow.Count)))
                .ToList();

            var current = Measure(transitions, currentWindow);
            var previous = priorWindow.Count == 0 ? null : Measure(transitions, priorWindow);
            var driftMagnitude = previous == null ? 0 : Distance(previous, current);
            var region = ClassifyRegion(transitions, current, driftMagnitude);

            return new StabilityAssessment
            {
                Current = current,
                Previous = previous,
                DriftMagnitude = driftMagnitude,
                Region = region,
                Confidence = ToConfidence(completed.Count),
                Recommendations = Recommend(region, current, driftMagnitude)
            };
        }

        private StabilityVector Measure(
            IReadOnlyList<Transition> allTransitions,
            IReadOnlyList<Transition> window)
        {
            var latest = window.Last();
            var baseline = window.First().Before;
            var current = latest.After;
            var driftScore = _driftDetector.Score(baseline, current).Score;

            return new StabilityVector
            {
                Identifiability = ScoreIdentifiability(window),
                Continuity = Clamp(100 - driftScore),
                Consistency = ScoreConsistency(allTransitions),
                Persistence = ScorePersistence(baseline, current),
                Recovery = ScoreRecovery(allTransitions)
            };
        }

        private static int ScoreIdentifiability(IReadOnlyList<Transition> transitions)
        {
            var actions = transitions.Where(t => t.ProposedAction != null).ToList();

            if (actions.Count == 0)
            {
                return 100;
            }

            var distinctCapabilities = actions
                .Select(t => t.ProposedAction.Capability)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();

            var rejectionPenalty = transitions.Count(t => t.Status == TransitionStatus.Rejected) * 15;
            var capabilitySpreadPenalty = Math.Max(0, distinctCapabilities - 3) * 5;

            return Clamp(100 - rejectionPenalty - capabilitySpreadPenalty);
        }

        private static int ScoreConsistency(IReadOnlyList<Transition> transitions)
        {
            var comparableGroups = transitions
                .Where(t => t.ProposedAction != null && t.Outcome != null)
                .GroupBy(
                    t => NormalizeComparableAction(t.ProposedAction),
                    StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .ToList();

            if (comparableGroups.Count == 0)
            {
                return 100;
            }

            var inconsistentGroups = comparableGroups.Count(g =>
                g.Select(t => OutcomeSignature(t))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count() > 1);

            return Clamp(100 - (inconsistentGroups * 100 / comparableGroups.Count));
        }

        private static int ScorePersistence(AgentState baseline, AgentState current)
        {
            var score = 100;

            if (!string.Equals(EffectiveGoal(baseline), EffectiveGoal(current), StringComparison.Ordinal))
            {
                score -= 30;
            }

            if (!string.Equals(baseline.Mode ?? "", current.Mode ?? "", StringComparison.Ordinal))
            {
                score -= 20;
            }

            var lostPolicies = baseline.Policies
                .Except(current.Policies, StringComparer.OrdinalIgnoreCase)
                .Count();

            score -= Math.Min(30, lostPolicies * 10);

            var baselineMemory = StableMemory(baseline);
            var currentMemory = StableMemory(current);
            var lostMemory = baselineMemory.Keys
                .Except(currentMemory.Keys, StringComparer.OrdinalIgnoreCase)
                .Count();

            score -= Math.Min(20, lostMemory * 10);

            return Clamp(score);
        }

        private static int ScoreRecovery(IReadOnlyList<Transition> transitions)
        {
            var rejectedTransitions = transitions
                .Where(t => t.Status == TransitionStatus.Rejected)
                .ToList();

            if (rejectedTransitions.Count == 0)
            {
                return 100;
            }

            var recovered = rejectedTransitions.Count(rejected =>
                transitions.Any(t =>
                    t.Timestamp > rejected.Timestamp &&
                    t.Status == TransitionStatus.Completed));

            return Clamp(recovered * 100 / rejectedTransitions.Count);
        }

        private static StabilityRegion ClassifyRegion(
            IReadOnlyList<Transition> transitions,
            StabilityVector vector,
            int driftMagnitude)
        {
            var hasInvariantFailure = transitions
                .SelectMany(t => t.Events)
                .Any(IsFailedInvariantEvent);

            if (hasInvariantFailure && (vector.AverageScore < 75 || driftMagnitude >= 25))
            {
                return StabilityRegion.Critical;
            }

            if (vector.AverageScore < 60 || driftMagnitude >= 40)
            {
                return StabilityRegion.Intervention;
            }

            if (vector.AverageScore < 85 || driftMagnitude >= 20 || hasInvariantFailure)
            {
                return StabilityRegion.Watch;
            }

            return StabilityRegion.Stable;
        }

        private static IReadOnlyList<string> Recommend(
            StabilityRegion region,
            StabilityVector vector,
            int driftMagnitude)
        {
            var recommendations = new List<string>();

            if (region == StabilityRegion.Stable)
            {
                recommendations.Add("Continue normal monitoring.");
            }
            else if (region == StabilityRegion.Watch)
            {
                recommendations.Add("Increase monitoring and review recent adaptive changes.");
            }
            else if (region == StabilityRegion.Intervention)
            {
                recommendations.Add("Run enhanced validation before approving further adaptation.");
            }
            else
            {
                recommendations.Add("Trigger human review before accepting additional behavioural changes.");
            }

            if (driftMagnitude >= 20)
            {
                recommendations.Add("Compare the latest stability vector with the approved baseline.");
            }

            if (vector.Recovery < 80)
            {
                recommendations.Add("Review rollback or recovery behaviour after rejected transitions.");
            }

            return recommendations;
        }

        private static MeasurementConfidence ToConfidence(int completedTransitions)
        {
            if (completedTransitions >= BehaviouralDriftDetector.DefaultHorizon * 2)
            {
                return MeasurementConfidence.High;
            }

            if (completedTransitions >= BehaviouralDriftDetector.DefaultHorizon)
            {
                return MeasurementConfidence.Medium;
            }

            return MeasurementConfidence.Low;
        }

        private static int Distance(StabilityVector left, StabilityVector right)
        {
            var sum =
                Math.Pow(left.Identifiability - right.Identifiability, 2) +
                Math.Pow(left.Continuity - right.Continuity, 2) +
                Math.Pow(left.Consistency - right.Consistency, 2) +
                Math.Pow(left.Persistence - right.Persistence, 2) +
                Math.Pow(left.Recovery - right.Recovery, 2);

            return Clamp((int)Math.Round(Math.Sqrt(sum)));
        }

        private static string NormalizeComparableAction(AgentAction action)
        {
            return $"{action.Capability ?? ""}:{(action.Input ?? "").Trim()}";
        }

        private static string OutcomeSignature(Transition transition)
        {
            if (transition.Outcome == null)
            {
                return "missing";
            }

            return transition.Outcome.Success
                ? $"ok:{transition.Outcome.Result}"
                : $"fail:{transition.Outcome.Error}";
        }

        private static string EffectiveGoal(AgentState state)
        {
            if (!string.IsNullOrWhiteSpace(state.Goal))
            {
                return state.Goal;
            }

            var memory = StableMemory(state);

            return memory.TryGetValue("goal", out var goal) ? goal : "";
        }

        private static IReadOnlyDictionary<string, string> StableMemory(AgentState state)
        {
            return state.Memory
                .Where(kvp => !VolatileMemoryKeys.Contains(kvp.Key))
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.ToString() ?? "",
                    StringComparer.OrdinalIgnoreCase);
        }

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

        private static int Clamp(int score)
        {
            return Math.Max(0, Math.Min(100, score));
        }
    }
}

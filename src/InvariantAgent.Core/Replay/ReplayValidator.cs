using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Drift;
using InvariantAgent.Core.Model.Control;
using InvariantAgent.Core.Model.Drift;
using InvariantAgent.Core.Model.Transition;
using System;
using System.Collections.Generic;
using System.Linq;

namespace InvariantAgent.Core.Replay;

public sealed class ReplayValidator
{
    private readonly IInvariantEvaluator _evaluator;

    private readonly DriftTracker _driftTracker;

    public ReplayValidator(IInvariantEvaluator evaluator, DriftTracker driftTracker)
    {
        _evaluator = evaluator;
        _driftTracker = driftTracker;
    }

    public ReplayValidationResult Validate(TransitionContext context)
    {
        var originalViolations = ExtractViolations(context.Transition);

        var scopes = GetScopesForReplay(context.Transition);

        var reports = scopes
            .Select(scope => _evaluator.Evaluate(context, scope))
            .ToList();

        var replayViolations = reports
            .Where(r => !r.Passed)
            .SelectMany(r => r.Violations)
            .Select(v => $"{v.Invariant}: {v.Reason}")
            .ToList();

        var replayRejected = reports.Any(r => !r.Passed);
        var originalRejected = context.Transition.Status == TransitionStatus.Rejected;

        var comparison = new ReplayComparison
        {
            OriginalStatus = context.Transition.Status,

            ReplayStatus = replayRejected ? TransitionStatus.Rejected : TransitionStatus.Completed,

            OriginalPhase = context.Transition.Phase,

            ReplayPhase = replayRejected ? TransitionPhase.Rejected : context.Transition.Phase,

            OriginalViolations = originalViolations,

            ReplayViolations = replayViolations
        };

        var score = 0;

        if (comparison.OriginalStatus != comparison.ReplayStatus)
        {
            score += 100;
        }

        if (comparison.OriginalPhase != comparison.ReplayPhase)
        {
            score += 50;
        }

        score += Math.Abs(comparison.OriginalViolations.Count - comparison.ReplayViolations.Count) * 10;

        //var severity = comparison.OriginalStatus != comparison.ReplayStatus
        //    ? InvariantSeverity.Critical
        //    : comparison.OriginalViolations.Count != comparison.ReplayViolations.Count
        //    ? InvariantSeverity.Error
        //    : InvariantSeverity.Info;

        var severity = score >= 100 ? InvariantSeverity.Critical :
                        score >= 50 ? InvariantSeverity.Error :
                        score > 0 ? InvariantSeverity.Warning : InvariantSeverity.Info;

        if (replayRejected != originalRejected)
        {
            var result = new ReplayValidationResult
            {
                Passed = false,
                DriftType = DriftType.GovernanceDrift,
                Reason = "Replay validation mismatch.",
                Comparison = comparison,
                Severity = severity
            };

            _driftTracker.Record(
                new DriftRecord
                {
                    Type = result.DriftType,
                    Reason = result.Reason,
                    TransitionId = context.Transition.Id.ToString(),
                    TimestampUtc = DateTime.UtcNow,
                    Phase = context.Transition.Phase,
                    Severity = severity
                });

            return result;
        }

        return new ReplayValidationResult
        {
            Passed = true,
            DriftType = DriftType.None,
            Comparison = comparison,
            Severity = severity
        };
    }

    private static IReadOnlyList<string> ExtractViolations(Transition transition)
    {
        return transition.Events
            .Where(e =>
                e.Stage == TransitionEventStage.Invariant &&
                e.Message.Contains("Failed"))
            .Select(e => e.Message)
            .ToList();
    }

    private static IReadOnlyList<InvariantScope> GetScopesForReplay(Transition transition)
    {
        if (transition.Status == TransitionStatus.Rejected)
        {
            if (HasInvariantFailure(transition, InvariantScope.Plan))
            {
                return new[] { InvariantScope.Plan };
            }

            if (HasInvariantFailure(transition, InvariantScope.Execution))
            {
                return new[] { InvariantScope.Plan, InvariantScope.Execution };
            }

            if (HasInvariantFailure(transition, InvariantScope.SelfModification))
            {
                return new[]
                {
                    InvariantScope.Plan,
                    InvariantScope.Execution,
                    InvariantScope.SelfModification
                };
            }

            if (HasInvariantFailure(transition, InvariantScope.Reduction))
            {
                return new[]
                {
                    InvariantScope.Plan,
                    InvariantScope.Execution,
                    InvariantScope.SelfModification,
                    InvariantScope.Reduction
                };
            }
        }

        return new[]
        {
            InvariantScope.Plan,
            InvariantScope.Execution,
            InvariantScope.SelfModification,
            InvariantScope.Reduction
        };
    }

    private static bool HasInvariantFailure(Transition transition, InvariantScope scope)
    {
        return transition.Events.Any(e =>
            e.Stage == TransitionEventStage.Invariant &&
            e.Message.Contains("Failed") &&
            e.Metadata.TryGetValue("Scope", out var value) &&
            string.Equals(
                value?.ToString(),
                scope.ToString(),
                StringComparison.OrdinalIgnoreCase));
    }
}
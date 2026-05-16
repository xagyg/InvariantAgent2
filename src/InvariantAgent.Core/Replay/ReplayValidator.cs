using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Drift;
using InvariantAgent.Core.Model.Control;
using InvariantAgent.Core.Model.Drift;
using InvariantAgent.Core.Model.Transition;
using System;
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
        var reports = new[]
        {
        _evaluator.Evaluate(context, InvariantScope.Plan),
        _evaluator.Evaluate(context, InvariantScope.Execution),
        _evaluator.Evaluate(context, InvariantScope.SelfModification),
        _evaluator.Evaluate(context, InvariantScope.Reduction)
    };

        var replayRejected = reports.Any(r => !r.Passed);
        var originalRejected = context.Transition.Status == TransitionStatus.Rejected;

        if (replayRejected != originalRejected)
        {
            var result = new ReplayValidationResult
            {
                Passed = false,
                DriftType = DriftType.GovernanceDrift,
                Reason = "Replay validation mismatch."
            };

            _driftTracker.Record(
                new DriftRecord
                {
                    Type = result.DriftType,
                    Reason = result.Reason,
                    TransitionId = context.Transition.Id.ToString(),
                    TimestampUtc = DateTime.UtcNow,
                    Phase = context.Transition.Phase,
                    Severity = InvariantSeverity.Critical
                });

            return result;
        }

        return new ReplayValidationResult
        {
            Passed = true,
            DriftType = DriftType.None
        };
    }
}
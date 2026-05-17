using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Drift;
using InvariantAgent.Core.Model.Control;
using InvariantAgent.Core.Model.Transition;

namespace InvariantAgent.Safety.Invariants.Identity;

public sealed class BehaviouralDriftBoundInvariant : IInvariant
{
    private readonly BehaviouralDriftDetector _detector;
    private readonly int _maxScore;

    public BehaviouralDriftBoundInvariant(
        BehaviouralDriftDetector detector,
        int maxScore = BehaviouralDriftDetector.DefaultBoundThreshold)
    {
        _detector = detector;
        _maxScore = maxScore;
    }

    public string Name => nameof(BehaviouralDriftBoundInvariant);

    public InvariantCategory Category => InvariantCategory.Identity;

    public InvariantScope Scope => InvariantScope.Reduction;

    public InvariantSeverity Severity => InvariantSeverity.Error;

    public InvariantResult Evaluate(TransitionContext context)
    {
        var transition = context.Transition;

        if (transition.After == null)
        {
            return InvariantResult.Allow();
        }

        var score = _detector.Score(transition);

        if (score.Score < _maxScore)
        {
            return InvariantResult.Allow();
        }

        return InvariantResult.Reject(
            $"Behavioural drift score {score.Score} exceeds bound {_maxScore}: {score.Explanation}",
            Severity);
    }
}

using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Drift;
using InvariantAgent.Core.Model.Control;
using InvariantAgent.Core.Model.Transition;

namespace InvariantAgent.Safety.Invariants.Identity;

/// <summary>
/// Reduction-scope identity invariant: rejects a proposed next state whose behavioural drift
/// score reaches the configured bound.
/// </summary>
/// <remarks>
/// This is where measurement becomes governance: drift is scored against the approved baseline
/// (or the prior state) before the new state is committed, bounding how far the agent may evolve
/// in one step.
/// </remarks>
public sealed class BehaviouralDriftBoundInvariant : IInvariant
{
    private readonly BehaviouralDriftDetector _detector;
    private readonly int _maxScore;

    /// <summary>Creates the invariant.</summary>
    /// <param name="detector">Detector used to score the proposed state.</param>
    /// <param name="maxScore">Score at or above which the transition is rejected.</param>
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

    /// <inheritdoc />
    public InvariantResult Evaluate(TransitionContext context)
    {
        var transition = context.Transition;

        if (transition.After == null)
        {
            return InvariantResult.Allow();
        }

        // Use the baseline recorded on the transition so replay reproduces the live decision.
        var score = _detector.ScoreAsRecorded(transition);

        if (score.Score < _maxScore)
        {
            return InvariantResult.Allow();
        }

        return InvariantResult.Reject(
            $"Behavioural drift score {score.Score} exceeds bound {_maxScore}: {score.Explanation}",
            Severity);
    }
}

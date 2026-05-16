using InvariantAgent.Core.Model.Drift;

namespace InvariantAgent.Core.Replay
{
    public sealed class ReplayValidationResult
    {
        public bool Passed { get; init; }

        public DriftType DriftType { get; init; }

        public string Reason { get; init; } = "";

        public ReplayComparison Comparison { get; init; }
    }
}

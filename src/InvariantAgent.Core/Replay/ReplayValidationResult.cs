using InvariantAgent.Core.Model.Control;
using InvariantAgent.Core.Model.Drift;

namespace InvariantAgent.Core.Replay
{
    public sealed class ReplayValidationResult
    {
        public bool Passed { get; init; }

        public DriftType DriftType { get; init; }

        public InvariantSeverity Severity { get; init; }

        public int Score { get; init; }

        public string Reason { get; init; } = "";

        public ReplayComparison Comparison { get; init; }
    }
}

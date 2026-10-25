using InvariantAgent.Core.Model.Control;
using InvariantAgent.Core.Model.Drift;

namespace InvariantAgent.Core.Replay
{
    /// <summary>Result of replay validation for a single transition.</summary>
    public sealed class ReplayValidationResult
    {
        /// <summary><c>true</c> if replay reproduced the original accept/reject decision.</summary>
        public bool Passed { get; init; }

        /// <summary><see cref="DriftType.GovernanceDrift"/> on mismatch, otherwise <see cref="DriftType.None"/>.</summary>
        public DriftType DriftType { get; init; }

        /// <summary>Severity derived from the mismatch score.</summary>
        public InvariantSeverity Severity { get; init; }

        /// <summary>Mismatch score (note: currently only recorded on the drift record, not set here).</summary>
        public int Score { get; init; }

        /// <summary>Explanation of the mismatch.</summary>
        public string Reason { get; init; } = "";

        /// <summary>Detailed original-vs-replay comparison.</summary>
        public ReplayComparison Comparison { get; init; }
    }
}

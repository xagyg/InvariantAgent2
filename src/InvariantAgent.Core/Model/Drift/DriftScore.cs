using InvariantAgent.Core.Model.Control;

namespace InvariantAgent.Core.Model.Drift
{
    /// <summary>A behavioural drift score with its severity band and explanation.</summary>
    public sealed class DriftScore
    {
        /// <summary>Numeric score (0 means no adaptive divergence).</summary>
        public int Score { get; init; }

        /// <summary>Severity band derived from the score.</summary>
        public InvariantSeverity Severity { get; init; }

        /// <summary>Human-readable description of what changed.</summary>
        public string Explanation { get; init; } = "";
    }
}

using System.Collections.Generic;

namespace InvariantAgent.Core.Model.Stability
{
    /// <summary>
    /// Result of behavioural stability evaluation (IBAAM 2). Indicates how behaviour is changing;
    /// invariants remain responsible for deciding whether change is acceptable.
    /// </summary>
    public sealed class StabilityAssessment
    {
        /// <summary>Stability vector for the most recent window of completed transitions.</summary>
        public StabilityVector Current { get; init; } = new();

        /// <summary>Stability vector for the preceding window, or <c>null</c> if there is not enough history.</summary>
        public StabilityVector Previous { get; init; }

        /// <summary>Euclidean distance between <see cref="Previous"/> and <see cref="Current"/> (0-100).</summary>
        public int DriftMagnitude { get; init; }

        /// <summary>Classified stability region.</summary>
        public StabilityRegion Region { get; init; } = StabilityRegion.Stable;

        /// <summary>Confidence in the measurement.</summary>
        public MeasurementConfidence Confidence { get; init; } = MeasurementConfidence.Low;

        /// <summary>Governance recommendations for the region.</summary>
        public IReadOnlyList<string> Recommendations { get; init; }
            = new List<string>();

        /// <summary><c>true</c> if a previous window was available for comparison.</summary>
        public bool HasPriorMeasurement => Previous != null;
    }
}

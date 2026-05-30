using System.Collections.Generic;

namespace InvariantAgent.Core.Model.Stability
{
    public sealed class StabilityAssessment
    {
        public StabilityVector Current { get; init; } = new();

        public StabilityVector Previous { get; init; }

        public int DriftMagnitude { get; init; }

        public StabilityRegion Region { get; init; } = StabilityRegion.Stable;

        public MeasurementConfidence Confidence { get; init; } = MeasurementConfidence.Low;

        public IReadOnlyList<string> Recommendations { get; init; }
            = new List<string>();

        public bool HasPriorMeasurement => Previous != null;
    }
}

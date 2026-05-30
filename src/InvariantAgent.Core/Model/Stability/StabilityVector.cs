namespace InvariantAgent.Core.Model.Stability
{
    public sealed class StabilityVector
    {
        public int Identifiability { get; init; } = 100;

        public int Continuity { get; init; } = 100;

        public int Consistency { get; init; } = 100;

        public int Persistence { get; init; } = 100;

        public int Recovery { get; init; } = 100;

        public double AverageScore =>
            (Identifiability + Continuity + Consistency + Persistence + Recovery) / 5.0;
    }
}

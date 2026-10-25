namespace InvariantAgent.Core.Model.Stability
{
    /// <summary>
    /// Agent Identity Evaluation (AIE) dimensions, each scored 0-100 (higher is more stable).
    /// </summary>
    public sealed class StabilityVector
    {
        /// <summary>How recognisable behaviour is: penalised by rejections and by a wide spread of capabilities.</summary>
        public int Identifiability { get; init; } = 100;

        /// <summary>How smoothly state evolves: 100 minus the behavioural drift score across the window.</summary>
        public int Continuity { get; init; } = 100;

        /// <summary>Whether identical actions yield identical outcomes.</summary>
        public int Consistency { get; init; } = 100;

        /// <summary>Whether goal, mode, policies and stable memory are retained across the window.</summary>
        public int Persistence { get; init; } = 100;

        /// <summary>Whether the agent returns to successful operation after rejected transitions.</summary>
        public int Recovery { get; init; } = 100;

        /// <summary>Unweighted mean of the five dimensions.</summary>
        public double AverageScore =>
            (Identifiability + Continuity + Consistency + Persistence + Recovery) / 5.0;
    }
}

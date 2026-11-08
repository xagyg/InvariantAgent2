namespace InvariantAgent.Core.Model.Control
{
    /// <summary>
    /// HIG-C criticality: the potential severity of consequences if an invariant is violated.
    /// Within a level, a more critical violation outweighs any amount of less critical violation (HIG-C).
    /// </summary>
    public enum InvariantCriticality
    {
        /// <summary>Minor consequences.</summary>
        Low = 1,
        /// <summary>Moderate consequences (default).</summary>
        Medium = 2,
        /// <summary>Significant consequences.</summary>
        High = 3,
        /// <summary>Severe consequences.</summary>
        Critical = 4,
        /// <summary>Irreversible or catastrophic consequences.</summary>
        Catastrophic = 5
    }
}

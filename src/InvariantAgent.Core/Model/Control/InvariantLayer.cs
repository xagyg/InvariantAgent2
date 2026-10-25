namespace InvariantAgent.Core.Model.Control
{
    /// <summary>
    /// HIG governance layers. Higher numeric values take precedence:
    /// Fundamental, then Mission, then Behavioural, then AdaptiveHeuristic.
    /// </summary>
    public enum InvariantLayer
    {
        /// <summary>Learned or tunable heuristics; lowest priority and most readily overridden.</summary>
        AdaptiveHeuristic = 1,
        /// <summary>Behavioural preferences and norms.</summary>
        Behavioural = 2,
        /// <summary>Obligations arising from the agent's mission or task.</summary>
        Mission = 3,
        /// <summary>Non-negotiable constraints. Never overridden.</summary>
        Fundamental = 4
    }
}

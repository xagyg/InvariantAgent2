namespace InvariantAgent.Core.Model.Stability
{
    /// <summary>Stability classification used to drive governance recommendations.</summary>
    public enum StabilityRegion
    {
        /// <summary>Behaviour within normal bounds; continue normal monitoring.</summary>
        Stable,
        /// <summary>Early signs of instability; increase monitoring.</summary>
        Watch,
        /// <summary>Significant instability; enhanced validation required before further adaptation.</summary>
        Intervention,
        /// <summary>Instability combined with invariant failures; human review required.</summary>
        Critical
    }
}

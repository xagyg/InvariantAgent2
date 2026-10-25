namespace InvariantAgent.Core.Model.Control
{
    /// <summary>HIG-C operational circumstances that influence same-layer conflict resolution.</summary>
    public enum OperationalContext
    {
        /// <summary>Normal operation (default).</summary>
        Normal,
        /// <summary>Operating with reduced capability or resources.</summary>
        Degraded,
        /// <summary>Responding to an emergency.</summary>
        Emergency,
        /// <summary>Recovering from a failure.</summary>
        Recovery,
        /// <summary>Planned maintenance.</summary>
        Maintenance
    }
}

namespace InvariantAgent.Core.Model.Agent
{
    /// <summary>
    /// The observed result of executing a proposed action (<c>Oₜ</c> in the IBAAM transition model).
    /// </summary>
    public sealed class AgentOutcome
    {
        /// <summary><c>true</c> if the capability executed successfully.</summary>
        public bool Success { get; init; }

        /// <summary>The capability that produced this outcome.</summary>
        public string Capability { get; init; } = "";

        /// <summary>String form of the capability's data on success; empty on failure.</summary>
        public string Result { get; init; } = "";

        /// <summary>Failure reason on unsuccessful execution.</summary>
        public string Error { get; init; } = "";
    }
}
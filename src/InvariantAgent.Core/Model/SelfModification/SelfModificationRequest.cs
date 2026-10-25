namespace InvariantAgent.Core.Model.SelfModification
{
    /// <summary>
    /// A capability's proposal to change agent state (e.g. <c>memory.set goal=...</c>).
    /// </summary>
    /// <remarks>
    /// Proposals are evaluated by self-modification invariants and, if approved, applied by the
    /// reducer. Capabilities never apply them directly.
    /// </remarks>
    public sealed class SelfModificationRequest
    {
        /// <summary>What is being modified (currently <c>memory</c>; future targets may include policy or planner).</summary>
        public string Target { get; init; } = "";

        /// <summary>The operation to perform (currently <c>set</c>).</summary>
        public string Operation { get; init; } = "";

        /// <summary>The key to modify.</summary>
        public string Key { get; init; } = "";

        /// <summary>The new value.</summary>
        public string Value { get; init; } = "";
    }
}
namespace InvariantAgent.Core.Model.Agent
{
    /// <summary>
    /// An action proposed by a planner: the capability to invoke and the input to pass to it
    /// (<c>Aₜ</c> in the IBAAM transition model). A proposal only - it is executed only after plan invariants pass.
    /// </summary>
    public class AgentAction
    {
        /// <summary>Name or alias of the capability to invoke.</summary>
        public string Capability { get; set; }
        /// <summary>Input passed to the capability.</summary>
        public string Input { get; set; }

        // null = success, non-null = failure reason
        /// <summary>Planner failure reason, if planning failed.</summary>
        public string Error { get; set; }

        /// <summary><c>true</c> if the planner reported an error.</summary>
        public bool HasError => !string.IsNullOrEmpty(Error);
    }
}
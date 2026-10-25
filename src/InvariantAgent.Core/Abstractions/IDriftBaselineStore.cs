using InvariantAgent.Core.Model.Agent;
using InvariantAgent.Core.Model.Drift;

namespace InvariantAgent.Core.Abstractions
{
    /// <summary>
    /// Holds the human-approved reference state against which behavioural drift is measured.
    /// </summary>
    /// <remarks>
    /// Approving a baseline lets an operator "accept" an evolved state so that subsequent drift
    /// is scored relative to it rather than to the previous transition.
    /// </remarks>
    public interface IDriftBaselineStore
    {
        /// <summary>The currently approved baseline, or <c>null</c> if none has been approved.</summary>
        DriftBaseline Current { get; }

        /// <summary>Approves a snapshot of <paramref name="state"/> as the new drift baseline.</summary>
        /// <param name="state">The committed agent state to approve.</param>
        /// <param name="reason">Human-readable justification recorded with the baseline.</param>
        /// <returns>The newly approved baseline.</returns>
        DriftBaseline Approve(AgentState state, string reason);
    }
}

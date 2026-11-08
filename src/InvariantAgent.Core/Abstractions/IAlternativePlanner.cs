using InvariantAgent.Core.Model.Agent;
using InvariantAgent.Core.Model.Planning;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace InvariantAgent.Core.Abstractions
{
    /// <summary>
    /// A planner that can offer alternatives to its proposal, enlarging the candidate set 𝒜 that HIG-C resolves over
    /// (IBAAM 3 revised, Section 6).
    /// </summary>
    /// <remarks>
    /// The runtime always adds the null action itself. Alternatives are listed in the planner's order of preference; when
    /// several candidates tie under the resolution rule, that order decides (the adaptive layer chooses within the governed
    /// set). Alternatives are proposals like any other: they are untrusted and pass every plan invariant before anything
    /// runs.
    /// </remarks>
    public interface IAlternativePlanner : IPlanner
    {
        /// <summary>Proposes alternatives to <paramref name="proposed"/>, most preferred first.</summary>
        /// <param name="context">The same bounded planner context the proposal was made from.</param>
        /// <param name="proposed">The planner's proposal.</param>
        /// <param name="ct">Cancellation token.</param>
        Task<IReadOnlyList<AgentAction>> ProposeAlternativesAsync(
            PlannerContext context,
            AgentAction proposed,
            CancellationToken ct = default);
    }
}

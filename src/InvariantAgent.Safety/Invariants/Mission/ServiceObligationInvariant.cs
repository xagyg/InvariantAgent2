using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Control;
using InvariantAgent.Core.Model.Transition;

namespace InvariantAgent.Safety.Invariants.Mission
{
    /// <summary>
    /// Mission-level duty to act on the user's request. Only the null action violates it: declining, holding or deferring
    /// leaves the request unanswered.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is what gives declining a cost under HIG-C (IBAAM 3 revised, Section 8.1). Without it, declining satisfies
    /// every invariant, so any violation at all makes declining the governed outcome and no override is ever possible
    /// (pure enforcement). With it, an action that violates only lower-level obligations (behavioural or heuristic) is
    /// better than declining and may proceed with witnessed overrides; an action that violates a mission obligation more
    /// critically than this one, or anything fundamental, still loses to declining.
    /// </para>
    /// <para>Severity is Warning: declining is always a permitted, safe hold, so its cost never needs authorising.</para>
    /// </remarks>
    public sealed class ServiceObligationInvariant : IInvariant
    {
        private readonly InvariantCriticality _criticality;

        /// <summary>Creates the invariant.</summary>
        /// <param name="criticality">How critical leaving a request unanswered is (default Medium).</param>
        public ServiceObligationInvariant(InvariantCriticality criticality = InvariantCriticality.Medium)
        {
            _criticality = criticality;
        }

        public string Name => nameof(ServiceObligationInvariant);

        public InvariantCategory Category => InvariantCategory.Integrity;

        /// <summary>Evaluated at every checkpoint: declining at any of them leaves the request unanswered.</summary>
        public InvariantScope Scope => InvariantScope.Transition;

        public InvariantSeverity Severity => InvariantSeverity.Warning;

        public InvariantLayer Layer => InvariantLayer.Mission;

        public InvariantCriticality Criticality => _criticality;

        public string ProtectedValue => "service";

        /// <summary>Acting on the request satisfies the obligation.</summary>
        public InvariantResult Evaluate(TransitionContext context)
        {
            return InvariantResult.Allow();
        }

        /// <summary>Declining leaves the request unanswered.</summary>
        public InvariantResult EvaluateDecline(TransitionContext context)
        {
            return InvariantResult.Reject("Declining leaves the user's request unanswered.", Severity);
        }
    }
}

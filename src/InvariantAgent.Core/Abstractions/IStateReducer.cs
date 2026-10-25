using InvariantAgent.Core.Model.Transition;

namespace InvariantAgent.Core.Abstractions
{
    /// <summary>
    /// Assimilates an approved transition into a proposed next state <c>Sₜ₊₁</c> (IBAAM state assimilation).
    /// </summary>
    public interface IStateReducer
    {
        /// <summary>
        /// Builds <c>context.Transition.After</c> from the prior state, the outcome and any approved
        /// self-modification. The proposed state is only committed by the runtime after reduction invariants pass.
        /// </summary>
        /// <param name="context">The transition being reduced.</param>
        void Apply(TransitionContext context);
    }
}
using InvariantAgent.Core.Model.Transition;

namespace InvariantAgent.Core.Transitioning
{
    /// <summary>
    /// Helpers for comparing a transition's current phase against the ordered <see cref="TransitionPhase"/> sequence.
    /// </summary>
    public static class TransitionLifecycle
    {
        /// <summary>Returns <c>true</c> if the transition is at or beyond <paramref name="phase"/>.</summary>
        public static bool HasReached(Transition transition, TransitionPhase phase)
        {
            return transition.Phase >= phase;
        }

        /// <summary>Returns <c>true</c> if the transition has not yet reached <paramref name="phase"/>.</summary>
        public static bool IsBefore(Transition transition, TransitionPhase phase)
        {
            return transition.Phase < phase;
        }

        /// <summary>Returns <c>true</c> if the transition is strictly beyond <paramref name="phase"/>.</summary>
        public static bool IsAfter(Transition transition, TransitionPhase phase)
        {
            return transition.Phase > phase;
        }
    }
}
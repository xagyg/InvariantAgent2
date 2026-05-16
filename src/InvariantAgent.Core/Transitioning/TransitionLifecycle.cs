using InvariantAgent.Core.Model.Transition;

namespace InvariantAgent.Core.Transitioning
{
    public static class TransitionLifecycle
    {
        public static bool HasReached(Transition transition, TransitionPhase phase)
        {
            return transition.Phase >= phase;
        }

        public static bool IsBefore(Transition transition, TransitionPhase phase)
        {
            return transition.Phase < phase;
        }

        public static bool IsAfter(Transition transition, TransitionPhase phase)
        {
            return transition.Phase > phase;
        }
    }
}
using InvariantAgent.Core.Model.Transition;
using System.Collections.Generic;

public static class TransitionPhases
{
    public static void MoveTo(Transition transition, TransitionPhase phase)
    {
        transition.Phase = phase;

        transition.AddEvent(TransitionEventStage.Lifecycle, $"Phase: {phase}",
            new Dictionary<string, object>
            {
                ["Phase"] = phase.ToString()
            });
    }
}
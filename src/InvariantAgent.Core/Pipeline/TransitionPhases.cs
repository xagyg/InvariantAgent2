using InvariantAgent.Core.Model.Transition;
using System.Collections.Generic;
using System;
using System.Linq;


namespace InvariantAgent.Core.Pipeline
{
    public static class TransitionPhases
    {
        private static readonly Dictionary<TransitionPhase, TransitionPhase[]> AllowedMoves =
            new Dictionary<TransitionPhase, TransitionPhase[]>
            {
                [TransitionPhase.Created] = new[]
                {
                    TransitionPhase.InputReceived,
                    TransitionPhase.Rejected
                },

                [TransitionPhase.InputReceived] = new[]
                {
                    TransitionPhase.Planning,
                    TransitionPhase.Rejected
                },

                [TransitionPhase.Planning] = new[]
                {
                    TransitionPhase.PlanValidation,
                    TransitionPhase.Rejected
                },

                [TransitionPhase.PlanValidation] = new[]
                {
                    TransitionPhase.Execution,
                    TransitionPhase.Rejected
                },

                [TransitionPhase.Execution] = new[]
                {
                    TransitionPhase.ExecutionValidation,
                    TransitionPhase.Rejected
                },

                [TransitionPhase.ExecutionValidation] = new[]
                {
                    TransitionPhase.SelfModificationValidation,
                    TransitionPhase.Reduction,
                    TransitionPhase.Rejected
                },

                [TransitionPhase.SelfModificationValidation] = new[]
                {
                    TransitionPhase.Reduction,
                    TransitionPhase.Rejected
                },

                [TransitionPhase.Reduction] = new[]
                {
                    TransitionPhase.Completed,
                    TransitionPhase.Rejected
                },

                [TransitionPhase.Completed] = Array.Empty<TransitionPhase>(),
                [TransitionPhase.Rejected] = Array.Empty<TransitionPhase>()
            };

        public static void MoveTo(Transition transition, TransitionPhase nextPhase)
        {
            var currentPhase = transition.Phase;

            if (!AllowedMoves.TryGetValue(currentPhase, out var allowed) || !allowed.Contains(nextPhase))
            {
                throw new InvalidOperationException($"Illegal transition phase move: {currentPhase} -> {nextPhase}");
            }

            transition.Phase = nextPhase;

            transition.AddEvent(TransitionEventStage.Lifecycle, $"Phase: {nextPhase}",
                new Dictionary<string, object>
                {
                    ["PreviousPhase"] = currentPhase.ToString(),
                    ["Phase"] = nextPhase.ToString()
                });
        }

        public static void Reject(Transition transition, string reason)
        {
            MoveTo(transition, TransitionPhase.Rejected);

            transition.Status = TransitionStatus.Rejected;
            transition.Reason = reason;
        }
    }
}
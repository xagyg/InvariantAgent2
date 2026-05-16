using InvariantAgent.Core.Model.Transition;
using System;
using System.Collections.Generic;

namespace InvariantAgent.Core.Replay
{
    public sealed class ReplayComparison
    {
        public TransitionStatus OriginalStatus { get; init; }

        public TransitionStatus ReplayStatus { get; init; }

        public TransitionPhase OriginalPhase { get; init; }

        public TransitionPhase ReplayPhase { get; init; }

        public IReadOnlyList<string> OriginalViolations { get; init; }
            = Array.Empty<string>();

        public IReadOnlyList<string> ReplayViolations { get; init; }
            = Array.Empty<string>();
    }
}

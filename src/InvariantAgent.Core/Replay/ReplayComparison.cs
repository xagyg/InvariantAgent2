using InvariantAgent.Core.Model.Transition;
using System;
using System.Collections.Generic;

namespace InvariantAgent.Core.Replay
{
    /// <summary>
    /// Side-by-side comparison of a transition's original governance decision and the decision obtained
    /// by re-evaluating invariants on replay.
    /// </summary>
    public sealed class ReplayComparison
    {
        /// <summary>Status recorded when the transition originally ran.</summary>
        public TransitionStatus OriginalStatus { get; init; }

        /// <summary>Status implied by re-evaluation (Rejected or Completed).</summary>
        public TransitionStatus ReplayStatus { get; init; }

        /// <summary>Phase recorded when the transition originally ran.</summary>
        public TransitionPhase OriginalPhase { get; init; }

        /// <summary>Phase implied by re-evaluation.</summary>
        public TransitionPhase ReplayPhase { get; init; }

        /// <summary>Failed-invariant messages from the original event trail.</summary>
        public IReadOnlyList<string> OriginalViolations { get; init; }
            = Array.Empty<string>();

        /// <summary>Unresolved violations found on replay.</summary>
        public IReadOnlyList<string> ReplayViolations { get; init; }
            = Array.Empty<string>();

        /// <summary>
        /// True if the transition carries the drift baseline it was originally scored against. When false
        /// (history recorded before baselines were captured), replay used the currently approved baseline,
        /// so a drift-related mismatch may reflect a baseline change rather than a rule change.
        /// </summary>
        public bool DriftBaselineRecorded { get; init; }
    }
}

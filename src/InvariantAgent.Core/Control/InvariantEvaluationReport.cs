using InvariantAgent.Core.Model.Agent;
using System.Collections.Generic;
using System;
using System.Linq;

namespace InvariantAgent.Core.Model.Control
{
    /// <summary>
    /// The outcome of resolving one lifecycle checkpoint under HIG-C.
    /// </summary>
    public sealed class InvariantEvaluationReport
    {
        /// <summary>The lifecycle scope that was evaluated.</summary>
        public InvariantScope Scope { get; init; }

        /// <summary>
        /// <c>true</c> when the runtime may continue with a governed action. Violations carried by that action appear as
        /// <see cref="Overrides"/>; they do not prevent the report from passing.
        /// </summary>
        public bool Passed => Violations.Count == 0;

        /// <summary>
        /// Violations that stop the transition: the in-flight action's violations when the governed outcome is to decline
        /// or to escalate.
        /// </summary>
        public IReadOnlyList<InvariantViolation> Violations { get; init; }
            = Array.Empty<InvariantViolation>();

        /// <summary>Violations carried by the governed action, each witnessed and authorised by the meta-invariants.</summary>
        public IReadOnlyList<InvariantOverride> Overrides { get; init; }
            = Array.Empty<InvariantOverride>();

        /// <summary>
        /// The alternative the resolution selected instead of the planner's proposal (plan checkpoint only); <c>null</c>
        /// when the proposal itself was selected or the outcome is to decline.
        /// </summary>
        public AgentAction SelectedAlternative { get; init; }

        /// <summary><c>true</c> when the governed outcome is the null action: the transition does not continue.</summary>
        public bool Declined { get; init; }

        /// <summary>
        /// <c>true</c> when the least-bad outcome violates a fundamental invariant and no pre-authorised protocol covers it,
        /// so the decision is held for human review (Section 7.2).
        /// </summary>
        public bool RequiresEscalation { get; init; }

        /// <summary>The full decision record: candidates, profiles, governed set and selection.</summary>
        public GovernanceResolution Resolution { get; init; }

        /// <summary>Human-readable summary used as the rejection reason and in control events.</summary>
        public string Summary =>
            Passed
                ? Overrides.Count == 0
                    ? "All invariants passed."
                    : "Governed action selected; its violations were witnessed and overridden."
                : (RequiresEscalation
                    ? "Escalation required: the least-bad outcome violates a fundamental invariant." + Environment.NewLine
                    : "") +
                  string.Join(
                    Environment.NewLine,
                    Violations.Select(v =>
                        $"{v.Invariant}: {v.Reason}"));
    }
}

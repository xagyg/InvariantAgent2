using System.Collections.Generic;
using System;

namespace InvariantAgent.Core.Model.Control
{
    /// <summary>
    /// A simple allow/block control decision, optionally derived from an <see cref="InvariantEvaluationReport"/>.
    /// </summary>
    public sealed class ControlDecision
    {
        /// <summary><c>true</c> if the transition may proceed.</summary>
        public bool Allowed { get; init; }

        /// <summary>Reason for blocking.</summary>
        public string Reason { get; init; } = "";

        /// <summary>Whether the decision was escalated (reserved for future human-in-the-loop handling).</summary>
        public bool Escalated { get; init; }

        /// <summary>Violations that caused the decision.</summary>
        public IReadOnlyList<InvariantViolation> Violations { get; init; } = Array.Empty<InvariantViolation>();    

        /// <summary>Creates an allowing decision.</summary>
        public static ControlDecision Allow()
        {
            return new ControlDecision
            {
                Allowed = true
            };
        }

        /// <summary>Creates a blocking decision.</summary>
        /// <param name="reason">Why the transition is blocked.</param>
        public static ControlDecision Block(string reason)
        {
            return new ControlDecision
            {
                Allowed = false,
                Reason = reason
            };
        }

        /// <summary>Converts an evaluation report into an allow/block decision.</summary>
        /// <param name="report">The invariant evaluation report.</param>
        public static ControlDecision From(InvariantEvaluationReport report)
        {
            if (report.Passed)
            {
                return Allow();
            }

            return Block(report.Summary);
        }
    }
}
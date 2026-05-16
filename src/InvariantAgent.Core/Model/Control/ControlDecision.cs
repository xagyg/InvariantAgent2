using System.Collections.Generic;
using System;

namespace InvariantAgent.Core.Model.Control
{
    public sealed class ControlDecision
    {
        public bool Allowed { get; init; }

        public string Reason { get; init; } = "";

        public bool Escalated { get; init; }

        public IReadOnlyList<InvariantViolation> Violations { get; init; } = Array.Empty<InvariantViolation>();    

        public static ControlDecision Allow()
        {
            return new ControlDecision
            {
                Allowed = true
            };
        }

        public static ControlDecision Block(string reason)
        {
            return new ControlDecision
            {
                Allowed = false,
                Reason = reason
            };
        }

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
using System;
using System.Collections.Generic;

namespace InvariantAgent.Core.Model.Control
{
    public sealed class ContextualGovernanceDecision
    {
        public ContextualGovernanceOutcome Outcome { get; init; }
            = ContextualGovernanceOutcome.NotApplicable;

        public InvariantCriticality Criticality { get; init; }
            = InvariantCriticality.Medium;

        public OperationalContext OperationalContext { get; init; }
            = OperationalContext.Normal;

        public IReadOnlyList<OperationalContext> Contexts { get; init; }
            = Array.Empty<OperationalContext>();

        public IReadOnlyList<string> ComparedInvariants { get; init; }
            = Array.Empty<string>();

        public string Reason { get; init; } = "";
    }
}

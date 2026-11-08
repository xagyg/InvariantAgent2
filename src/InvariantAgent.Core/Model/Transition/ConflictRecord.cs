using InvariantAgent.Core.Model.Control;
using System;
using System.Collections.Generic;
using System.Linq;

namespace InvariantAgent.Core.Model.Transition
{
    /// <summary>
    /// A compact, persisted summary of one resolved invariant conflict (IBAAM 3 revised, Section 7.4). The full decision
    /// record (every candidate profile and witness) is in the transition's Control events.
    /// </summary>
    public sealed class ConflictRecord
    {
        /// <summary>The checkpoint at which the conflict was resolved.</summary>
        public InvariantScope Scope { get; set; }

        /// <summary>Operational context the decision was made under.</summary>
        public OperationalContext OperationalContext { get; set; }

        /// <summary>The selected candidate: <c>proposed</c>, <c>alternative-N</c> or <c>decline</c>.</summary>
        public string Selected { get; set; } = GovernanceCandidate.ProposedId;

        /// <summary>Capability of the selected candidate; empty when it is the decline option.</summary>
        public string SelectedCapability { get; set; } = "";

        /// <summary>The selected candidate's violation profile.</summary>
        public string Profile { get; set; } = "none";

        /// <summary>Invariants the selected action overrides.</summary>
        public List<string> Overridden { get; set; } = new();

        /// <summary>
        /// <c>true</c> when the resolution prescribes different behaviour from simply carrying out the proposal (an
        /// alternative, a decline or overrides), i.e. a behavioural change is expected.
        /// </summary>
        public bool ChangesBehaviour { get; set; }

        /// <summary><c>true</c> when the conflict was held for human review.</summary>
        public bool Escalated { get; set; }

        /// <summary>Plain-words reason for the selection.</summary>
        public string Reason { get; set; } = "";

        /// <summary>Builds a record from an evaluation report; <c>null</c> when nothing was resolved.</summary>
        public static ConflictRecord From(InvariantEvaluationReport report)
        {
            var resolution = report.Resolution;

            if (resolution == null || (!resolution.Conflict && report.Overrides.Count == 0 && report.SelectedAlternative == null))
            {
                return null;
            }

            var selected = resolution.Candidates.FirstOrDefault(c => c.Id == resolution.SelectedId);

            return new ConflictRecord
            {
                Scope = report.Scope,
                OperationalContext = resolution.OperationalContext,
                Selected = resolution.SelectedId,
                SelectedCapability = selected?.Action?.Capability ?? "",
                Profile = selected?.Profile ?? "none",
                Overridden = report.Overrides.Select(o => o.OverriddenViolation.Invariant).ToList(),
                ChangesBehaviour = resolution.SelectedId != GovernanceCandidate.ProposedId || report.Overrides.Count > 0,
                Escalated = report.RequiresEscalation,
                Reason = resolution.Reason
            };
        }
    }
}

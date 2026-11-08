using InvariantAgent.Core.Model.Transition;
using System;
using System.Collections.Generic;

namespace InvariantAgent.Core.Model.Control
{
    /// <summary>
    /// Input to a meta-invariant: one violation the governed candidate would carry, plus the resolution around it.
    /// </summary>
    public sealed class MetaInvariantContext
    {
        /// <summary>The transition being governed.</summary>
        public TransitionContext TransitionContext { get; init; } = default!;

        /// <summary>The violation being considered for override.</summary>
        public InvariantViolation CandidateViolation { get; init; } = new();

        /// <summary>All violations of the candidate action in the same checkpoint.</summary>
        public IReadOnlyList<InvariantViolation> AllViolations { get; init; }
            = Array.Empty<InvariantViolation>();

        /// <summary>All invariant evaluations (passed and failed) of the candidate action in the same checkpoint.</summary>
        public IReadOnlyList<InvariantEvaluationRecord> EvaluationRecords { get; init; }
            = Array.Empty<InvariantEvaluationRecord>();

        /// <summary>Id of the candidate being authorised (see <see cref="GovernanceCandidate"/>).</summary>
        public string CandidateId { get; init; } = GovernanceCandidate.ProposedId;

        /// <summary>The candidate's violation profile.</summary>
        public ViolationProfile CandidateProfile { get; init; } = ViolationProfile.Zero;

        /// <summary>
        /// Candidates that would have violated <see cref="CandidateViolation"/>'s invariant less, with the position at which
        /// each is worse (Section 7.1).
        /// </summary>
        public IReadOnlyList<OverrideWitness> Witnesses { get; init; } = Array.Empty<OverrideWitness>();

        /// <summary>Number of candidates in the governed set (more than one means a tie left to the adaptive layer).</summary>
        public int GovernedSetSize { get; init; } = 1;

        /// <summary>Operational context the decision is made under.</summary>
        public OperationalContext OperationalContext { get; init; }

        /// <summary>Pre-authorised protocol covering a fundamental-level violation, or empty when none does.</summary>
        public string EscalationProtocol { get; init; } = "";
    }
}

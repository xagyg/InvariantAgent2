using InvariantAgent.Core.Model.Agent;
using System;
using System.Collections.Generic;

namespace InvariantAgent.Core.Model.Control
{
    /// <summary>
    /// The decision record for one governed checkpoint (IBAAM 3 revised, Section 7.4): every candidate and its violation
    /// profile, the governed set R(𝒜, κ), the action selected within it and any escalation.
    /// </summary>
    /// <remarks>
    /// Because the resolution rule is deterministic given the context, the violation degrees and the criticality table, a
    /// reviewer can recompute the decision from this record.
    /// </remarks>
    public sealed class GovernanceResolution
    {
        /// <summary>The checkpoint that was resolved.</summary>
        public InvariantScope Scope { get; init; }

        /// <summary>Operational context κ the decision was made under.</summary>
        public OperationalContext OperationalContext { get; init; }

        /// <summary>Every candidate considered, in adaptive-preference order (proposal first, decline last).</summary>
        public IReadOnlyList<GovernanceCandidate> Candidates { get; init; } = Array.Empty<GovernanceCandidate>();

        /// <summary>Ids of the candidates in the governed set R(𝒜, κ): those with lexicographically minimal profiles.</summary>
        public IReadOnlyList<string> GovernedSet { get; init; } = Array.Empty<string>();

        /// <summary>Id of the candidate the runtime will act on (<see cref="GovernanceCandidate.DeclineId"/> when it holds).</summary>
        public string SelectedId { get; init; } = GovernanceCandidate.ProposedId;

        /// <summary><c>true</c> when no candidate satisfies every invariant.</summary>
        public bool Conflict { get; init; }

        /// <summary>
        /// <c>true</c> when the minimal profile has a positive fundamental component, so no governed action may be carried
        /// out autonomously (Section 7.2).
        /// </summary>
        public bool RequiresEscalation { get; init; }

        /// <summary>The pre-authorised protocol that covered the escalation, or empty when none did.</summary>
        public string EscalationProtocol { get; init; } = "";

        /// <summary>Why this candidate was selected, in plain words.</summary>
        public string Reason { get; init; } = "";
    }

    /// <summary>One candidate action and its evaluation under the resolution rule.</summary>
    public sealed class GovernanceCandidate
    {
        /// <summary>Id of the planner's proposal (the action in flight at later checkpoints).</summary>
        public const string ProposedId = "proposed";

        /// <summary>Id of the null action a₀: decline, hold or defer.</summary>
        public const string DeclineId = "decline";

        /// <summary>Candidate id: <c>proposed</c>, <c>alternative-N</c> or <c>decline</c>.</summary>
        public string Id { get; init; } = ProposedId;

        /// <summary>The candidate action; <c>null</c> for the decline option.</summary>
        public AgentAction Action { get; init; }

        /// <summary><c>true</c> for the null action a₀.</summary>
        public bool IsDecline => Id == DeclineId;

        /// <summary>The candidate's violation profile, rendered (see <see cref="ViolationProfile.ToString"/>).</summary>
        public string Profile { get; init; } = "none";

        /// <summary>The invariants this candidate violates, with degree and criticality in context.</summary>
        public IReadOnlyList<InvariantViolation> Violations { get; init; } = Array.Empty<InvariantViolation>();

        /// <summary><c>true</c> when the candidate is in the governed set.</summary>
        public bool Governed { get; init; }

        /// <summary>
        /// <c>true</c> when the candidate is governed and every override it needs is authorised by the meta-invariants.
        /// </summary>
        public bool Valid { get; init; }

        /// <summary>Why a governed candidate was not valid (meta-invariant rejections), if it was not.</summary>
        public IReadOnlyList<string> Rejections { get; init; } = Array.Empty<string>();
    }

    /// <summary>
    /// Evidence that an override was necessary (Section 7.1): a candidate that would have violated the overridden invariant
    /// less, and the profile position at which that candidate is worse.
    /// </summary>
    public sealed class OverrideWitness
    {
        /// <summary>Id of the competing candidate.</summary>
        public string Candidate { get; init; } = "";

        /// <summary>The competing candidate's violation degree for the overridden invariant.</summary>
        public double CompetingDegree { get; init; }

        /// <summary>Level of the profile component at which the competing candidate is worse.</summary>
        public InvariantLayer DecidingLayer { get; init; }

        /// <summary>Criticality of the profile component at which the competing candidate is worse.</summary>
        public InvariantCriticality DecidingCriticality { get; init; }

        /// <summary>Invariants the competing candidate violates at the deciding position.</summary>
        public IReadOnlyList<string> DecidingInvariants { get; init; } = Array.Empty<string>();

        /// <summary>
        /// <c>false</c> when the competing candidate has an equal profile, i.e. it is not worse and the override is not
        /// witnessed.
        /// </summary>
        public bool Worse { get; init; } = true;

        /// <inheritdoc />
        public override string ToString()
        {
            return Worse
                ? $"{Candidate} is worse at {ViolationProfile.Short(DecidingLayer)}/{DecidingCriticality}" +
                  (DecidingInvariants.Count > 0 ? $" ({string.Join(", ", DecidingInvariants)})" : "")
                : $"{Candidate} is equally good and violates it less";
        }
    }
}

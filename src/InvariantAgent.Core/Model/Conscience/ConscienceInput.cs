using System;
using System.Collections.Generic;
using InvariantAgent.Core.Model.Agent;
using InvariantAgent.Core.Model.Stability;

namespace InvariantAgent.Core.Model.Conscience
{
    /// <summary>How a precedent transition ended, as far as the controls are concerned.</summary>
    public enum PrecedentOutcome
    {
        /// <summary>Every control passed and the transition completed.</summary>
        Passed,
        /// <summary>A control (invariant) blocked or rejected it. A boundary label.</summary>
        Blocked,
        /// <summary>
        /// The conscience stopped it before the controls saw all of it, so there is no boundary label. Excluded from
        /// violation risk to avoid the feedback loop in IBAAM 6, Section 9.
        /// </summary>
        Withheld
    }

    /// <summary>
    /// A similar past transition retrieved for precedent-based reasoning (IBAAM 6, Section 6.4.1): "this plan resembles
    /// transition X, blocked by Y for protecting Z".
    /// </summary>
    /// <param name="TransitionId">The past transition.</param>
    /// <param name="Similarity">Plan similarity in 0..1.</param>
    /// <param name="Outcome">How the controls treated it.</param>
    /// <param name="Capability">Its capability.</param>
    /// <param name="Input">Its capability input.</param>
    public sealed record Precedent(
        Guid TransitionId,
        double Similarity,
        PrecedentOutcome Outcome,
        string Capability,
        string Input)
    {
        /// <summary>Invariants that blocked it (when <see cref="Outcome"/> is Blocked).</summary>
        public IReadOnlyList<string> BlockedBy { get; init; } = Array.Empty<string>();

        /// <summary>Protected-value tags of the invariants that blocked it.</summary>
        public IReadOnlyList<string> Values { get; init; } = Array.Empty<string>();

        /// <summary>Behavioural drift score its resulting state produced, if it reached reduction.</summary>
        public int? RealisedDrift { get; init; }
    }

    /// <summary>
    /// Everything a conscience may see when appraising a plan (IBAAM 6, Section 6.1).
    /// </summary>
    /// <remarks>
    /// The appraisal must be a function of this input alone, so the same plan and the same history always give the
    /// same appraisal (determinism serves replay and audit), while a different history can give a different one
    /// (history sensitivity, the property that separates a conscience from an invariant).
    /// </remarks>
    /// <param name="Point">Plan or answer appraisal.</param>
    /// <param name="Proposed">The action that will actually run (after plan invariants).</param>
    /// <param name="State">The last committed agent state.</param>
    /// <param name="Trend">Behavioural stability over recent history.</param>
    /// <param name="Precedents">Similar past transitions, most similar first.</param>
    /// <param name="Commitments">Live commitments in scope for this plan.</param>
    public sealed record ConscienceInput(
        AppraisalPoint Point,
        AgentAction Proposed,
        AgentState State,
        StabilityAssessment Trend,
        IReadOnlyList<Precedent> Precedents,
        IReadOnlyList<Commitment> Commitments)
    {
        /// <summary>Smoothed rate at which the controls have blocked plans in this deployment (the cold-start prior).</summary>
        public double BaseRate { get; init; } = 0.25;

        /// <summary>Number of labelled transitions (passed or blocked) the base rate was computed from.</summary>
        public int LabelledHistory { get; init; }

        /// <summary>
        /// Answer point only: the drift score of the proposed next state against the baseline in force (a dry run of
        /// the commit). <c>null</c> at the plan point, where drift is forecast from precedents.
        /// </summary>
        public int? ForecastDriftScore { get; init; }

        /// <summary>Drift score at which the drift-bound invariant rejects; used to normalise drift.</summary>
        public int DriftBound { get; init; } = 70;

        /// <summary>Answer point only: the observed outcome.</summary>
        public AgentOutcome Outcome { get; init; }

        /// <summary>Revision attempt (0 for the planner's first proposal).</summary>
        public int Attempt { get; init; }
    }
}

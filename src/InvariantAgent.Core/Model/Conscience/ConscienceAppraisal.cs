using System;
using System.Collections.Generic;

namespace InvariantAgent.Core.Model.Conscience
{
    /// <summary>
    /// The result of a conscience appraisal (IBAAM 6, Section 6.1): graded, decomposed tension plus a stance.
    /// </summary>
    /// <param name="Tension">
    /// Combined tension in 0..1. A weighted score used to select a stance; it is not a probability
    /// (only the violation-risk part is calibrated).
    /// </param>
    /// <param name="Parts">The decomposed tension components.</param>
    /// <param name="Stance">Proceed, Revise, Escalate or Veto.</param>
    /// <param name="Justification">Generated from <paramref name="Parts"/> and the cited cases.</param>
    /// <param name="CitedPrecedents">Ids of the past transitions the appraisal reasons from.</param>
    public sealed record ConscienceAppraisal(
        double Tension,
        IReadOnlyList<TensionComponent> Parts,
        ConscienceStance Stance,
        string Justification,
        IReadOnlyList<Guid> CitedPrecedents)
    {
        /// <summary>Ids of the live commitments the plan was found to conflict with (or be cleared by).</summary>
        public IReadOnlyList<Guid> CitedCommitments { get; init; } = Array.Empty<Guid>();

        /// <summary>Protected-value tags at stake, most significant first.</summary>
        public IReadOnlyList<string> ValuesAtStake { get; init; } = Array.Empty<string>();

        /// <summary>Name of the conscience implementation that produced the appraisal.</summary>
        public string Conscience { get; init; } = "";
    }
}

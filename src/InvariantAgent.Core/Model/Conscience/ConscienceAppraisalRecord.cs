using System;

namespace InvariantAgent.Core.Model.Conscience
{
    /// <summary>What the runtime did with an appraisal.</summary>
    /// <remarks>Numeric values are persisted, so new members must only ever be appended.</remarks>
    public enum ConscienceEffect
    {
        /// <summary>The pipeline continued unchanged.</summary>
        Proceeded,
        /// <summary>The appraisal was returned to the planner and a revised plan was requested.</summary>
        Revised,
        /// <summary>The transition was held for human review; nothing further ran.</summary>
        Escalated,
        /// <summary>The transition was rejected by the conscience.</summary>
        Vetoed,
        /// <summary>Answer point: the answer was allowed with a caveat recorded (Revise at the answer point).</summary>
        Caveated
    }

    /// <summary>
    /// An appraisal as recorded on its transition, so deterministic replay reproduces the live decision even where a
    /// component is non-deterministic (IBAAM 6, Section 6.4.4).
    /// </summary>
    public sealed class ConscienceAppraisalRecord
    {
        /// <summary>Plan or answer appraisal.</summary>
        public AppraisalPoint Point { get; init; }

        /// <summary>Revision attempt the appraisal was made on (0 = the planner's first proposal).</summary>
        public int Attempt { get; init; }

        /// <summary>Capability of the action that was appraised.</summary>
        public string Capability { get; init; } = "";

        /// <summary>Input of the action that was appraised.</summary>
        public string Input { get; init; } = "";

        /// <summary>The appraisal itself.</summary>
        public ConscienceAppraisal Appraisal { get; init; }

        /// <summary>What the runtime did with it. Can differ from the stance (e.g. Revise at the limit becomes Escalated).</summary>
        public ConscienceEffect Effect { get; init; }

        /// <summary>Why the effect differs from the stance, if it does.</summary>
        public string Note { get; init; } = "";

        /// <summary>Commitment created from this appraisal, if any.</summary>
        public Guid? CommitmentId { get; init; }

        /// <summary><c>true</c> when the effect stopped the transition (Escalated or Vetoed).</summary>
        public bool Stopped => Effect == ConscienceEffect.Escalated || Effect == ConscienceEffect.Vetoed;

        /// <summary>When the appraisal was made.</summary>
        public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
    }
}

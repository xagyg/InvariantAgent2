using System;
using System.Collections.Generic;

namespace InvariantAgent.Core.Model.Conscience
{
    /// <summary>The four components of prospective tension (IBAAM 6, Section 6.2).</summary>
    /// <remarks>Numeric values are persisted, so new members must only ever be appended.</remarks>
    public enum TensionKind
    {
        /// <summary>
        /// Probability that similar plans were later blocked or rejected by the controls. The only component that is
        /// a calibrated probability.
        /// </summary>
        ViolationRisk,

        /// <summary>Drift the plan's resulting state would produce, relative to the drift bound.</summary>
        ForecastDrift,

        /// <summary>Remaining tolerance given recent behaviour (the current stability region).</summary>
        TrendPressure,

        /// <summary>Inconsistency with the agent's own live commitments and reviewer decisions.</summary>
        CommitmentConflict
    }

    /// <summary>
    /// One decomposed part of a conscience appraisal. The justification is generated from these parts, so the
    /// stated reason always matches the computed score (IBAAM 6, Section 6.4.3).
    /// </summary>
    /// <param name="Kind">Which tension component this is.</param>
    /// <param name="Value">Component value in 0..1.</param>
    /// <param name="Weight">Weight used when combining components into the overall tension.</param>
    /// <param name="Detail">Human-readable account of how the value was computed.</param>
    public sealed record TensionComponent(
        TensionKind Kind,
        double Value,
        double Weight,
        string Detail)
    {
        /// <summary>Protected-value tags this component concerns (from invariants or commitments).</summary>
        public IReadOnlyList<string> Values { get; init; } = Array.Empty<string>();

        /// <summary><c>false</c> when the component does not apply at this appraisal point and was not combined.</summary>
        public bool Applicable { get; init; } = true;
    }
}

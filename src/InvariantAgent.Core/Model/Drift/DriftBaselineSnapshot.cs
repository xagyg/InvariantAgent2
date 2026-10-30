using InvariantAgent.Core.Model.Agent;
using System;
using System.Text.Json.Serialization;

namespace InvariantAgent.Core.Model.Drift
{
    /// <summary>
    /// The drift baseline a transition was scored against, captured on the transition itself.
    /// </summary>
    /// <remarks>
    /// Self-contained (it carries the baseline state, not just its id) so replay is deterministic even if the
    /// baseline store is in-memory, has been reset, or has since approved a different baseline.
    /// </remarks>
    public sealed class DriftBaselineSnapshot
    {
        /// <summary>Id of the approved baseline, or <c>null</c> if none was approved (the transition's <c>Before</c> state was used).</summary>
        public Guid? BaselineId { get; init; }

        /// <summary>State version of the approved baseline, if any.</summary>
        public int? StateVersion { get; init; }

        /// <summary>When the baseline was approved, if any.</summary>
        public DateTime? ApprovedAtUtc { get; init; }

        /// <summary>The approved baseline state, or <c>null</c> when no baseline was approved.</summary>
        public AgentState State { get; init; }

        /// <summary>True when an operator-approved baseline was in force.</summary>
        [JsonIgnore]
        public bool HasApprovedBaseline => BaselineId != null;

        /// <summary>Captures <paramref name="baseline"/> (which may be <c>null</c>, meaning "no approved baseline").</summary>
        public static DriftBaselineSnapshot From(DriftBaseline baseline)
        {
            if (baseline == null)
            {
                return new DriftBaselineSnapshot();
            }

            return new DriftBaselineSnapshot
            {
                BaselineId = baseline.Id,
                StateVersion = baseline.StateVersion,
                ApprovedAtUtc = baseline.ApprovedAtUtc,
                State = baseline.State
            };
        }
    }
}

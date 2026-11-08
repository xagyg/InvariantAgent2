using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Control;
using System.Collections.Generic;
using System.Linq;

namespace InvariantAgent.Core.Control.MetaInvariants
{
    /// <summary>
    /// Flags overrides that warrant human review without blocking them (IBAAM 3 revised, Section 7.5).
    /// </summary>
    /// <remarks>
    /// Review is requested when:
    /// <list type="bullet">
    /// <item>the override was decided within a level, by criticality in context (a same-level conflict);</item>
    /// <item>several candidates tie in the governed set, so the adaptive layer's preference decided;</item>
    /// <item>a fundamental violation was authorised by a pre-authorised protocol;</item>
    /// <item>the number of simultaneous violations reaches the configured threshold.</item>
    /// </list>
    /// Repeated overrides of the same invariant across transitions are an observability concern and are reported by the
    /// drift tooling rather than here.
    /// </remarks>
    public sealed class ReviewMetaInvariant : IMetaInvariant
    {
        /// <summary>Number of simultaneous violations at or above which review is requested.</summary>
        private readonly int _violationReviewThreshold;

        /// <summary>Creates the meta-invariant.</summary>
        /// <param name="violationReviewThreshold">Violation count that triggers review (default 3).</param>
        public ReviewMetaInvariant(int violationReviewThreshold = 3)
        {
            _violationReviewThreshold = violationReviewThreshold;
        }

        public string Name => nameof(ReviewMetaInvariant);

        public MetaInvariantCategory Category => MetaInvariantCategory.Review;

        /// <inheritdoc />
        public MetaInvariantResult Evaluate(MetaInvariantContext context)
        {
            var reasons = new List<string>();
            var violation = context.CandidateViolation;

            var sameLevel = context.Witnesses
                .Where(w => w.Worse && w.DecidingLayer == violation.Layer)
                .ToList();

            if (sameLevel.Count > 0)
            {
                reasons.Add(
                    $"{violation.Invariant} was decided within level {violation.Layer} by criticality in " +
                    $"{context.OperationalContext}: " + string.Join("; ", sameLevel));
            }

            if (context.GovernedSetSize > 1)
            {
                reasons.Add(
                    $"{context.GovernedSetSize} candidates tie in the governed set; the adaptive layer's preference decided.");
            }

            if (violation.Layer == InvariantLayer.Fundamental && !string.IsNullOrEmpty(context.EscalationProtocol))
            {
                reasons.Add(
                    $"Fundamental invariant {violation.Invariant} overridden under protocol '{context.EscalationProtocol}'.");
            }

            if (context.AllViolations.Count >= _violationReviewThreshold)
            {
                reasons.Add(
                    $"Override considered while {context.AllViolations.Count} invariants were violated.");
            }

            return MetaInvariantResult.Allow(
                metadata: new Dictionary<string, object>
                {
                    ["RequiresReview"] = reasons.Count > 0,
                    ["ReviewReasons"] = reasons.ToArray()
                });
        }
    }
}

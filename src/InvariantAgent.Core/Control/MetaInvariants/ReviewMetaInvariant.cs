using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Control;
using System.Collections.Generic;
using System.Linq;

namespace InvariantAgent.Core.Control.MetaInvariants
{
    public sealed class ReviewMetaInvariant : IMetaInvariant
    {
        private readonly int _violationReviewThreshold;

        public ReviewMetaInvariant(int violationReviewThreshold = 3)
        {
            _violationReviewThreshold = violationReviewThreshold;
        }

        public string Name => nameof(ReviewMetaInvariant);

        public MetaInvariantCategory Category => MetaInvariantCategory.Review;

        public MetaInvariantResult Evaluate(MetaInvariantContext context)
        {
            var reasons = new List<string>();
            var violation = context.CandidateViolation;

            var sameLayerConflicts = context.AllViolations
                .Where(v =>
                    v.Invariant != violation.Invariant &&
                    v.Layer == violation.Layer)
                .Select(v => v.Invariant)
                .ToArray();

            if (sameLayerConflicts.Length > 0)
            {
                reasons.Add(
                    $"{violation.Invariant} conflicts with same-layer invariant(s): " +
                    string.Join(", ", sameLayerConflicts));
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

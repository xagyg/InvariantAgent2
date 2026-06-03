using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Control;
using System.Collections.Generic;
using System.Linq;

namespace InvariantAgent.Core.Control.MetaInvariants
{
    public sealed class PriorityMetaInvariant : IMetaInvariant
    {
        public string Name => nameof(PriorityMetaInvariant);

        public MetaInvariantCategory Category => MetaInvariantCategory.Priority;

        public MetaInvariantResult Evaluate(MetaInvariantContext context)
        {
            var violation = context.CandidateViolation;

            if (violation.Layer == InvariantLayer.Fundamental)
            {
                return MetaInvariantResult.Reject(
                    "Fundamental invariants are non-overrideable.");
            }

            var higherPriorityFailure = context.AllViolations
                .FirstOrDefault(v => v.Layer > violation.Layer);

            if (higherPriorityFailure != null)
            {
                return MetaInvariantResult.Reject(
                    $"{violation.Invariant} cannot be overridden while higher-priority invariant " +
                    $"{higherPriorityFailure.Invariant} is unresolved.");
            }

            var preservedHigherPriorityInvariants = context.EvaluationRecords
                .Where(r => r.Result.Passed && r.Invariant.Layer > violation.Layer)
                .Select(r => r.Invariant.Name)
                .ToArray();

            if (preservedHigherPriorityInvariants.Length == 0)
            {
                return MetaInvariantResult.Reject(
                    $"{violation.Invariant} has no preserved higher-priority invariant to justify override authority.");
            }

            return MetaInvariantResult.Allow(
                metadata: new Dictionary<string, object>
                {
                    ["PreservedHigherPriorityInvariants"] = preservedHigherPriorityInvariants
                });
        }
    }
}

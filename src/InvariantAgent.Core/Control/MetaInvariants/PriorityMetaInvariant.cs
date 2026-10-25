using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Control;
using System.Collections.Generic;
using System.Linq;

namespace InvariantAgent.Core.Control.MetaInvariants
{
    /// <summary>
    /// Enforces HIG layer precedence (Fundamental, then Mission, then Behavioural, then AdaptiveHeuristic).
    /// </summary>
    /// <remarks>
    /// A violation may only be overridden when: it is not in the Fundamental layer; no higher-layer
    /// invariant is also violated; and at least one higher-layer invariant was preserved (passed).
    /// The preserved invariants are returned as <c>PreservedHigherPriorityInvariants</c> metadata.
    /// </remarks>
    public sealed class PriorityMetaInvariant : IMetaInvariant
    {
        public string Name => nameof(PriorityMetaInvariant);

        public MetaInvariantCategory Category => MetaInvariantCategory.Priority;

        /// <inheritdoc />
        public MetaInvariantResult Evaluate(MetaInvariantContext context)
        {
            var violation = context.CandidateViolation;

            // Rule 1: fundamental invariants are never overrideable.
            if (violation.Layer == InvariantLayer.Fundamental)
            {
                return MetaInvariantResult.Reject(
                    "Fundamental invariants are non-overrideable.");
            }

            // Rule 2: a lower-layer violation cannot be waived while a higher-layer one is unresolved.
            var higherPriorityFailure = context.AllViolations
                .FirstOrDefault(v => v.Layer > violation.Layer);

            if (higherPriorityFailure != null)
            {
                return MetaInvariantResult.Reject(
                    $"{violation.Invariant} cannot be overridden while higher-priority invariant " +
                    $"{higherPriorityFailure.Invariant} is unresolved.");
            }

            // Rule 3: override authority comes from a higher-layer invariant that actually passed.
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

using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Control;
using System.Collections.Generic;
using System.Linq;

namespace InvariantAgent.Core.Control.MetaInvariants
{
    /// <summary>
    /// Requires an inspectable rationale for every override.
    /// </summary>
    /// <remarks>
    /// An override is only justifiable when at least one higher-layer invariant was evaluated and
    /// passed. The generated justification names those preserved invariants and is returned as
    /// <c>Justification</c> metadata.
    /// </remarks>
    public sealed class JustificationMetaInvariant : IMetaInvariant
    {
        public string Name => nameof(JustificationMetaInvariant);

        public MetaInvariantCategory Category => MetaInvariantCategory.Justification;

        /// <inheritdoc />
        public MetaInvariantResult Evaluate(MetaInvariantContext context)
        {
            var violation = context.CandidateViolation;
            // Higher-priority = a numerically greater InvariantLayer that passed in this evaluation.
            var preservedHigherPriorityInvariants = context.EvaluationRecords
                .Where(r => r.Result.Passed && r.Invariant.Layer > violation.Layer)
                .Select(r => r.Invariant.Name)
                .ToArray();

            if (preservedHigherPriorityInvariants.Length == 0)
            {
                return MetaInvariantResult.Reject(
                    "Override justification requires preserved higher-priority invariants.");
            }

            var justification =
                $"{violation.Invariant} was overridden because higher-priority invariants " +
                $"{string.Join(", ", preservedHigherPriorityInvariants)} were preserved.";

            return MetaInvariantResult.Allow(
                metadata: new Dictionary<string, object>
                {
                    ["Justification"] = justification
                });
        }
    }
}

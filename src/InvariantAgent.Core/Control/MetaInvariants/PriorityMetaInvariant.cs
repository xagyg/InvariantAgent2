using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Control;
using System.Collections.Generic;

namespace InvariantAgent.Core.Control.MetaInvariants
{
    /// <summary>
    /// Fundamental invariants and escalation (IBAAM 3 revised, Section 7.2): no action that violates a fundamental
    /// invariant may be carried out autonomously.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Priority between levels is enforced by the resolution rule itself: a higher-level violation always outweighs a
    /// lower-level one in the violation profile, so this meta-invariant no longer needs the earlier rules ("no override
    /// while a higher-level violation is unresolved", "override authority from a preserved higher-level invariant").
    /// </para>
    /// <para>
    /// What remains is the fundamental rule. A fundamental violation is authorised only when a pre-authorised protocol
    /// covers it; otherwise the agent holds at the null action and the evaluator escalates. Changing priorities is a change
    /// to the hierarchy (versioned and approved), never a runtime override.
    /// </para>
    /// </remarks>
    public sealed class PriorityMetaInvariant : IMetaInvariant
    {
        public string Name => nameof(PriorityMetaInvariant);

        public MetaInvariantCategory Category => MetaInvariantCategory.Priority;

        /// <inheritdoc />
        public MetaInvariantResult Evaluate(MetaInvariantContext context)
        {
            var violation = context.CandidateViolation;

            if (violation.Layer == InvariantLayer.Fundamental)
            {
                if (string.IsNullOrEmpty(context.EscalationProtocol))
                {
                    return MetaInvariantResult.Reject(
                        $"{violation.Invariant} is fundamental: it cannot be violated autonomously and requires escalation.");
                }

                return MetaInvariantResult.Allow(
                    $"Fundamental violation authorised by pre-authorised protocol '{context.EscalationProtocol}'.",
                    new Dictionary<string, object>
                    {
                        ["EscalationProtocol"] = context.EscalationProtocol
                    });
            }

            return MetaInvariantResult.Allow();
        }
    }
}

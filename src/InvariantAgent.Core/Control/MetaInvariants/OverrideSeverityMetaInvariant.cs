using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Control;

namespace InvariantAgent.Core.Control.MetaInvariants
{
    /// <summary>
    /// Deployment policy: <see cref="InvariantSeverity.Error"/> and <see cref="InvariantSeverity.Critical"/> violations
    /// are never overridden. Only Info and Warning violations are eligible.
    /// </summary>
    /// <remarks>
    /// The HIG-C paper has no severity dimension; this is an additional, deployment-level meta-invariant kept so that
    /// invariants declared as hard constraints stay hard regardless of level. A governed candidate that would need a hard
    /// override is not valid, and the runtime falls back to the next governed candidate or to declining. Remove it from the
    /// meta-invariant set to let level, criticality and context alone decide.
    /// </remarks>
    public sealed class OverrideSeverityMetaInvariant : IMetaInvariant
    {
        public string Name => nameof(OverrideSeverityMetaInvariant);

        public MetaInvariantCategory Category => MetaInvariantCategory.OverrideSeverity;

        /// <inheritdoc />
        public MetaInvariantResult Evaluate(MetaInvariantContext context)
        {
            var violation = context.CandidateViolation;

            if (violation.Severity >= InvariantSeverity.Error)
            {
                return MetaInvariantResult.Reject(
                    $"{violation.Invariant} has severity {violation.Severity} and remains blocking by policy.");
            }

            return MetaInvariantResult.Allow();
        }
    }
}

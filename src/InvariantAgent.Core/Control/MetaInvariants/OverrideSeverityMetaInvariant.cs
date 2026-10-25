using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Control;

namespace InvariantAgent.Core.Control.MetaInvariants
{
    /// <summary>
    /// Keeps <see cref="InvariantSeverity.Error"/> and <see cref="InvariantSeverity.Critical"/> violations
    /// blocking. Only Info and Warning violations are eligible for override by default.
    /// </summary>
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
                    $"{violation.Invariant} has severity {violation.Severity} and remains blocking by default.");
            }

            return MetaInvariantResult.Allow();
        }
    }
}

using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Control;

namespace InvariantAgent.Core.Control.MetaInvariants
{
    public sealed class OverrideSeverityMetaInvariant : IMetaInvariant
    {
        public string Name => nameof(OverrideSeverityMetaInvariant);

        public MetaInvariantCategory Category => MetaInvariantCategory.OverrideSeverity;

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

using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Control;
using System.Collections.Generic;
using System.Linq;

namespace InvariantAgent.Core.Control.MetaInvariants
{
    /// <summary>
    /// Witnessed override (IBAAM 3 revised, Section 7.1): an override is authorised only when every candidate that would
    /// have violated the invariant less has a worse violation profile.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The witnesses are computed by the evaluator and supplied in <see cref="MetaInvariantContext.Witnesses"/>. Because
    /// the candidate is in the governed set, every competing candidate is at least as bad; the override is rejected when a
    /// competitor is <em>equally</em> good and violates the invariant less, since the override is then not necessary.
    /// </para>
    /// <para>
    /// This replaces the earlier rule that justified an override whenever some higher-level invariant had passed, which
    /// allowed overrides with no conflict at all. The generated justification is returned as <c>Justification</c> metadata.
    /// </para>
    /// </remarks>
    public sealed class JustificationMetaInvariant : IMetaInvariant
    {
        public string Name => nameof(JustificationMetaInvariant);

        public MetaInvariantCategory Category => MetaInvariantCategory.Justification;

        /// <inheritdoc />
        public MetaInvariantResult Evaluate(MetaInvariantContext context)
        {
            var violation = context.CandidateViolation;
            var unwitnessed = context.Witnesses.Where(w => !w.Worse).ToList();

            if (unwitnessed.Count > 0)
            {
                return MetaInvariantResult.Reject(
                    $"{violation.Invariant} override is not witnessed: " +
                    string.Join(", ", unwitnessed.Select(w => w.Candidate)) +
                    " is equally good and violates it less.");
            }

            var justification = context.Witnesses.Count == 0
                ? $"{violation.Invariant} (degree {violation.Degree:0.###}) is overridden: no candidate violates it less."
                : $"{violation.Invariant} (degree {violation.Degree:0.###}, {violation.CriticalityInContext} in " +
                  $"{context.OperationalContext}) is overridden because every alternative that violates it less is worse: " +
                  string.Join("; ", context.Witnesses) + ".";

            return MetaInvariantResult.Allow(
                metadata: new Dictionary<string, object>
                {
                    ["Justification"] = justification
                });
        }
    }
}

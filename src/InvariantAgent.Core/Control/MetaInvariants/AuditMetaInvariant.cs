using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Control;
using System.Collections.Generic;

namespace InvariantAgent.Core.Control.MetaInvariants
{
    /// <summary>
    /// Marks every authorised override for governance audit.
    /// </summary>
    /// <remarks>Never rejects; contributes <c>Audit = true</c> metadata to the override record.</remarks>
    public sealed class AuditMetaInvariant : IMetaInvariant
    {
        public string Name => nameof(AuditMetaInvariant);

        public MetaInvariantCategory Category => MetaInvariantCategory.Audit;

        /// <inheritdoc />
        public MetaInvariantResult Evaluate(MetaInvariantContext context)
        {
            return MetaInvariantResult.Allow(
                metadata: new Dictionary<string, object>
                {
                    ["Audit"] = true
                });
        }
    }
}

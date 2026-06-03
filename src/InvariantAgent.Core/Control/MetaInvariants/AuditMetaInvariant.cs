using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Control;
using System.Collections.Generic;

namespace InvariantAgent.Core.Control.MetaInvariants
{
    public sealed class AuditMetaInvariant : IMetaInvariant
    {
        public string Name => nameof(AuditMetaInvariant);

        public MetaInvariantCategory Category => MetaInvariantCategory.Audit;

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

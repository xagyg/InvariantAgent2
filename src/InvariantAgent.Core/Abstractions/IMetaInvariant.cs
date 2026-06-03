using InvariantAgent.Core.Model.Control;

namespace InvariantAgent.Core.Abstractions
{
    public interface IMetaInvariant
    {
        string Name { get; }

        MetaInvariantCategory Category { get; }

        MetaInvariantResult Evaluate(MetaInvariantContext context);
    }
}

using InvariantAgent.Core.Model.Control;
using InvariantAgent.Core.Model.Transition;

namespace InvariantAgent.Core.Abstractions
{
    public interface IInvariant
    {
        string Name { get; }

        InvariantCategory Category { get; }

        InvariantScope Scope { get; }

        InvariantSeverity Severity { get; }

        InvariantLayer Layer => InvariantLayer.Fundamental;

        InvariantCriticality Criticality => InvariantCriticality.Medium;

        System.Collections.Generic.IReadOnlyList<OperationalContext> Contexts =>
            System.Array.Empty<OperationalContext>();

        InvariantResult Evaluate(TransitionContext context);
    }
}

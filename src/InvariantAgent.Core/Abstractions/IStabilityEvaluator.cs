using InvariantAgent.Core.Model.Stability;
using InvariantAgent.Core.Model.Transition;
using System.Collections.Generic;

namespace InvariantAgent.Core.Abstractions
{
    public interface IStabilityEvaluator
    {
        StabilityAssessment Evaluate(IReadOnlyList<Transition> transitions);
    }
}

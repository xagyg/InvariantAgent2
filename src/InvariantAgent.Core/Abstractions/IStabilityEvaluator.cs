using InvariantAgent.Core.Model.Stability;
using InvariantAgent.Core.Model.Transition;
using System.Collections.Generic;

namespace InvariantAgent.Core.Abstractions
{
    /// <summary>
    /// Measures behavioural stability over transition history using Agent Identity Evaluation (AIE)
    /// style metrics (IBAAM 2). Measurement indicates change; invariants still decide acceptability.
    /// </summary>
    public interface IStabilityEvaluator
    {
        /// <summary>Computes a stability assessment for the supplied transitions.</summary>
        /// <param name="transitions">Ordered transition history.</param>
        /// <returns>The current stability vector, drift magnitude, region and recommendations.</returns>
        StabilityAssessment Evaluate(IReadOnlyList<Transition> transitions);
    }
}

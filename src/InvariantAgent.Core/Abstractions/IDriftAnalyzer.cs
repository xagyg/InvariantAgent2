using InvariantAgent.Core.Model.Drift;
using InvariantAgent.Core.Model.Transition;
using System.Collections.Generic;

namespace InvariantAgent.Core.Abstractions
{
    /// <summary>
    /// Produces an aggregate <see cref="DriftReport"/> from transition history (IBAAM Observability layer).
    /// </summary>
    public interface IDriftAnalyzer
    {
        /// <summary>Analyses the supplied transitions for drift, invariant failures, capability usage and stability.</summary>
        /// <param name="transitions">Ordered transition history to analyse.</param>
        /// <returns>A drift report summarising behavioural change across the history.</returns>
        DriftReport Analyze(IReadOnlyList<Transition> transitions);
    }
}
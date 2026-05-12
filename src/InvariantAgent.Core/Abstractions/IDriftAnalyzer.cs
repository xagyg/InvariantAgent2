using InvariantAgent.Core.Model.Drift;
using InvariantAgent.Core.Model.Transition;
using System.Collections.Generic;

namespace InvariantAgent.Core.Abstractions
{
    public interface IDriftAnalyzer
    {
        DriftReport Analyze(IReadOnlyList<Transition> transitions);
    }
}
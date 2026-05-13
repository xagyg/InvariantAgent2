using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Control;
using InvariantAgent.Core.Model.Transition;
using System.Collections.Generic;

namespace InvariantAgent.Core.Control.Pre
{
    public sealed class PreControl : IPreControl
    {
        private readonly IEnumerable<IInvariant> _invariants;

        public PreControl(IEnumerable<IInvariant> invariants)
        {
            _invariants = invariants;
        }

        public ControlDecision Evaluate(TransitionContext context)
        {
            foreach (var invariant in _invariants)
            {
                var result = invariant.Evaluate(context);

                context.Transition.AddEvent(TransitionEventStage.PreInvariant,
                    $"{invariant.Name}: {(result.Passed ? "Passed" : "Failed")} {result.Reason}",
                    new Dictionary<string, object>()
                    {
                        ["Invariant"] = invariant.Name,
                        ["Passed"] = result.Passed,
                        ["Reason"] = result.Reason
                    });

                if (!result.Passed)
                {
                    context.Transition.Status = TransitionStatus.Rejected;

                    context.Transition.Reason =
                        $"Invariant '{invariant.Name}' failed: {result.Reason}";

                    return ControlDecision.Block(context.Transition.Reason);
                }
            }

            context.Transition.Status = TransitionStatus.Allowed;

            return ControlDecision.Allow();
        }
    }
}
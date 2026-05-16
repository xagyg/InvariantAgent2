using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Control;
using InvariantAgent.Core.Model.Transition;
using System.Collections.Generic;
using System.Linq;

namespace InvariantAgent.Core.Control
{
    public sealed class InvariantEvaluator : IInvariantEvaluator
    {
        private readonly IReadOnlyList<IInvariant> _invariants;

        public InvariantEvaluator(IEnumerable<IInvariant> invariants)
        {
            _invariants = invariants.ToList();
        }

        public InvariantEvaluationReport Evaluate(TransitionContext context, InvariantScope scope)
        {
            var violations = new List<InvariantViolation>();

            foreach (var invariant in _invariants.Where(i =>
                i.Scope == scope ||
                i.Scope == InvariantScope.Transition))
            {
                var result = invariant.Evaluate(context);

                context.Transition.AddEvent(
                    TransitionEventStage.Invariant,
                    $"{invariant.Name}: {(result.Passed ? "Passed" : "Failed")} {result.Reason}",
                    new Dictionary<string, object>
                    {
                        ["Invariant"] = invariant.Name,
                        ["Category"] = invariant.Category.ToString(),
                        ["Scope"] = invariant.Scope.ToString(),
                        ["Severity"] = invariant.Severity.ToString(),
                        ["Passed"] = result.Passed,
                        ["Reason"] = result.Reason
                    });

                if (!result.Passed)
                {
                    violations.Add(new InvariantViolation
                    {
                        Invariant = invariant.Name,
                        Category = invariant.Category,
                        Scope = invariant.Scope,
                        Severity = invariant.Severity,
                        Reason = result.Reason
                    });
                }
            }

            return new InvariantEvaluationReport
            {
                Scope = scope,
                Violations = violations
            };
        }
    }
}
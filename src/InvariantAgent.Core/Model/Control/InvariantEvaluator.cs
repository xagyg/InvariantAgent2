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

        private sealed class EvaluationRecord
        {
            public IInvariant Invariant { get; init; } = default!;

            public InvariantResult Result { get; init; } = default!;
        }

        public InvariantEvaluator(IEnumerable<IInvariant> invariants)
        {
            _invariants = invariants.ToList();
        }

        public InvariantEvaluationReport Evaluate(TransitionContext context, InvariantScope scope)
        {
            var records = new List<EvaluationRecord>();

            foreach (var invariant in _invariants.Where(i =>
                i.Scope == scope ||
                i.Scope == InvariantScope.Transition))
            {
                var result = invariant.Evaluate(context);
                records.Add(new EvaluationRecord
                {
                    Invariant = invariant,
                    Result = result
                });

                context.Transition.AddEvent(
                    TransitionEventStage.Invariant,
                    $"{invariant.Name}: {(result.Passed ? "Passed" : "Failed")} {result.Reason}",
                    new Dictionary<string, object>
                    {
                        ["Invariant"] = invariant.Name,
                        ["Category"] = invariant.Category.ToString(),
                        ["Scope"] = invariant.Scope.ToString(),
                        ["Severity"] = invariant.Severity.ToString(),
                        ["Layer"] = invariant.Layer.ToString(),
                        ["Passed"] = result.Passed,
                        ["Reason"] = result.Reason
                    });
            }

            var allViolations = records
                .Where(r => !r.Result.Passed)
                .Select(r => new InvariantViolation
                {
                    Invariant = r.Invariant.Name,
                    Category = r.Invariant.Category,
                    Scope = r.Invariant.Scope,
                    Severity = r.Invariant.Severity,
                    Layer = r.Invariant.Layer,
                    Reason = r.Result.Reason
                })
                .ToList();

            var overrides = ResolveOverrides(context, records, allViolations);
            var overriddenNames = overrides
                .Select(o => o.OverriddenViolation.Invariant)
                .ToHashSet();
            var unresolvedViolations = allViolations
                .Where(v => !overriddenNames.Contains(v.Invariant))
                .ToList();

            return new InvariantEvaluationReport
            {
                Scope = scope,
                Violations = unresolvedViolations,
                Overrides = overrides
            };
        }

        private static IReadOnlyList<InvariantOverride> ResolveOverrides(
            TransitionContext context,
            IReadOnlyList<EvaluationRecord> records,
            IReadOnlyList<InvariantViolation> violations)
        {
            var overrides = new List<InvariantOverride>();

            foreach (var violation in violations)
            {
                if (violation.Layer == InvariantLayer.Fundamental ||
                    violation.Severity >= InvariantSeverity.Error)
                {
                    continue;
                }

                var higherPriorityFailures = violations
                    .Where(v => v.Layer > violation.Layer)
                    .ToList();

                if (higherPriorityFailures.Count > 0)
                {
                    continue;
                }

                var preservedHigherPriorityInvariants = records
                    .Where(r => r.Result.Passed && r.Invariant.Layer > violation.Layer)
                    .Select(r => r.Invariant.Name)
                    .ToArray();

                if (preservedHigherPriorityInvariants.Length == 0)
                {
                    continue;
                }

                var justification =
                    $"{violation.Invariant} was overridden because higher-priority invariants " +
                    $"{string.Join(", ", preservedHigherPriorityInvariants)} were preserved.";

                var overrideDecision = new InvariantOverride
                {
                    OverriddenViolation = violation,
                    PreservedHigherPriorityInvariants = preservedHigherPriorityInvariants,
                    Justification = justification
                };

                overrides.Add(overrideDecision);

                context.Transition.AddEvent(
                    TransitionEventStage.Control,
                    $"Override authorised: {violation.Invariant}",
                    new Dictionary<string, object>
                    {
                        ["MetaInvariantCategory"] = overrideDecision.MetaInvariantCategory,
                        ["OverriddenInvariant"] = violation.Invariant,
                        ["OverriddenLayer"] = violation.Layer.ToString(),
                        ["PreservedHigherPriorityInvariants"] = preservedHigherPriorityInvariants,
                        ["Justification"] = justification,
                        ["Audit"] = true
                    });
            }

            return overrides;
        }
    }
}

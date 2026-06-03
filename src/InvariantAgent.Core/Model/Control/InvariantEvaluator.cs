using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Control.MetaInvariants;
using InvariantAgent.Core.Model.Control;
using InvariantAgent.Core.Model.Transition;
using System.Collections.Generic;
using System.Linq;

namespace InvariantAgent.Core.Control
{
    public sealed class InvariantEvaluator : IInvariantEvaluator
    {
        private readonly IReadOnlyList<IInvariant> _invariants;
        private readonly IReadOnlyList<IMetaInvariant> _metaInvariants;

        private sealed class MetaEvaluation
        {
            public IMetaInvariant MetaInvariant { get; init; } = default!;

            public MetaInvariantResult Result { get; init; } = default!;
        }

        public InvariantEvaluator(IEnumerable<IInvariant> invariants)
            : this(invariants, DefaultMetaInvariants())
        {
        }

        public InvariantEvaluator(
            IEnumerable<IInvariant> invariants,
            IEnumerable<IMetaInvariant> metaInvariants)
        {
            _invariants = invariants.ToList();
            _metaInvariants = metaInvariants.ToList();
        }

        public InvariantEvaluationReport Evaluate(TransitionContext context, InvariantScope scope)
        {
            var records = new List<InvariantEvaluationRecord>();

            foreach (var invariant in _invariants.Where(i =>
                i.Scope == scope ||
                i.Scope == InvariantScope.Transition))
            {
                var result = invariant.Evaluate(context);
                records.Add(new InvariantEvaluationRecord
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

        private IReadOnlyList<InvariantOverride> ResolveOverrides(
            TransitionContext context,
            IReadOnlyList<InvariantEvaluationRecord> records,
            IReadOnlyList<InvariantViolation> violations)
        {
            var overrides = new List<InvariantOverride>();

            foreach (var violation in violations)
            {
                var metaContext = new MetaInvariantContext
                {
                    TransitionContext = context,
                    CandidateViolation = violation,
                    AllViolations = violations,
                    EvaluationRecords = records
                };

                var metaEvaluations = _metaInvariants
                    .Select(m => new MetaEvaluation
                    {
                        MetaInvariant = m,
                        Result = m.Evaluate(metaContext)
                    })
                    .ToList();

                if (metaEvaluations.Any(e => !e.Result.Passed))
                {
                    continue;
                }

                var preservedHigherPriorityInvariants =
                    GetMetadata<string[]>(metaEvaluations, "PreservedHigherPriorityInvariants")
                    ?? new string[0];
                var justification =
                    GetMetadata<string>(metaEvaluations, "Justification")
                    ?? $"{violation.Invariant} was overridden by meta-invariant policy.";
                var audit = GetMetadata<bool>(metaEvaluations, "Audit");
                var categories = metaEvaluations
                    .Select(e => e.MetaInvariant.Category)
                    .Distinct()
                    .ToArray();

                var overrideDecision = new InvariantOverride
                {
                    OverriddenViolation = violation,
                    PreservedHigherPriorityInvariants = preservedHigherPriorityInvariants,
                    Justification = justification,
                    MetaInvariantCategory = string.Join(", ", categories),
                    MetaInvariantCategories = categories
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
                        ["Audit"] = audit,
                        ["MetaInvariants"] = metaEvaluations
                            .Select(e => e.MetaInvariant.Name)
                            .ToArray()
                    });
            }

            return overrides;
        }

        private static IReadOnlyList<IMetaInvariant> DefaultMetaInvariants()
        {
            return new IMetaInvariant[]
            {
                new PriorityMetaInvariant(),
                new OverrideSeverityMetaInvariant(),
                new JustificationMetaInvariant(),
                new AuditMetaInvariant()
            };
        }

        private static T GetMetadata<T>(
            IEnumerable<MetaEvaluation> metaEvaluations,
            string key)
        {
            foreach (var evaluation in metaEvaluations)
            {
                if (evaluation.Result.Metadata.TryGetValue(key, out var value) &&
                    value is T typed)
                {
                    return typed;
                }
            }

            return default(T);
        }
    }
}

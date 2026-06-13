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
                        ["Criticality"] = invariant.Criticality.ToString(),
                        ["Contexts"] = invariant.Contexts.Select(c => c.ToString()).ToArray(),
                        ["OperationalContext"] = context.OperationalContext.ToString(),
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
                    Criticality = r.Invariant.Criticality,
                    Contexts = r.Invariant.Contexts,
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
                var contextualDecision = ResolveContextualDecision(
                    context.OperationalContext,
                    violation,
                    violations);

                if (contextualDecision.Outcome == ContextualGovernanceOutcome.Preserve)
                {
                    context.Transition.AddEvent(
                        TransitionEventStage.Control,
                        $"HIG-C preserved same-layer invariant: {violation.Invariant}",
                        new Dictionary<string, object>
                        {
                            ["Invariant"] = violation.Invariant,
                            ["Layer"] = violation.Layer.ToString(),
                            ["Criticality"] = violation.Criticality.ToString(),
                            ["OperationalContext"] = context.OperationalContext.ToString(),
                            ["ComparedInvariants"] = contextualDecision.ComparedInvariants,
                            ["Reason"] = contextualDecision.Reason
                        });

                    continue;
                }

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
                var requiresReview = GetMetadata<bool>(metaEvaluations, "RequiresReview");
                var reviewReasons =
                    GetMetadata<string[]>(metaEvaluations, "ReviewReasons")
                    ?? new string[0];
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
                    MetaInvariantCategories = categories,
                    RequiresReview = requiresReview,
                    ReviewReasons = reviewReasons,
                    ContextualDecision = contextualDecision
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
                        ["Criticality"] = violation.Criticality.ToString(),
                        ["OperationalContext"] = context.OperationalContext.ToString(),
                        ["ContextualGovernanceOutcome"] = contextualDecision.Outcome.ToString(),
                        ["ContextualGovernanceReason"] = contextualDecision.Reason,
                        ["ComparedInvariants"] = contextualDecision.ComparedInvariants,
                        ["PreservedHigherPriorityInvariants"] = preservedHigherPriorityInvariants,
                        ["Justification"] = justification,
                        ["Audit"] = audit,
                        ["RequiresReview"] = requiresReview,
                        ["ReviewReasons"] = reviewReasons,
                        ["MetaInvariants"] = metaEvaluations
                            .Select(e => e.MetaInvariant.Name)
                            .ToArray()
                    });
            }

            return overrides;
        }

        private static ContextualGovernanceDecision ResolveContextualDecision(
            OperationalContext operationalContext,
            InvariantViolation violation,
            IReadOnlyList<InvariantViolation> violations)
        {
            var sameLayerConflicts = violations
                .Where(v =>
                    v.Invariant != violation.Invariant &&
                    v.Layer == violation.Layer)
                .ToArray();

            if (sameLayerConflicts.Length == 0)
            {
                return new ContextualGovernanceDecision
                {
                    Outcome = ContextualGovernanceOutcome.NotApplicable,
                    Criticality = violation.Criticality,
                    OperationalContext = operationalContext,
                    Contexts = violation.Contexts,
                    Reason = "No equal-priority conflict was detected."
                };
            }

            var candidateScore = ContextualScore(violation, operationalContext);
            var conflictScores = sameLayerConflicts
                .Select(v => new
                {
                    Violation = v,
                    Score = ContextualScore(v, operationalContext)
                })
                .ToArray();
            var highestConflictScore = conflictScores.Max(v => v.Score);
            var compared = sameLayerConflicts
                .Select(v => v.Invariant)
                .ToArray();

            if (candidateScore > highestConflictScore)
            {
                return new ContextualGovernanceDecision
                {
                    Outcome = ContextualGovernanceOutcome.Preserve,
                    Criticality = violation.Criticality,
                    OperationalContext = operationalContext,
                    Contexts = violation.Contexts,
                    ComparedInvariants = compared,
                    Reason =
                        $"{violation.Invariant} has the strongest HIG-C score for {operationalContext} " +
                        "and remains unresolved."
                };
            }

            if (candidateScore < highestConflictScore)
            {
                var strongerInvariants = conflictScores
                    .Where(v => v.Score > candidateScore)
                    .Select(v => v.Violation.Invariant)
                    .ToArray();

                return new ContextualGovernanceDecision
                {
                    Outcome = ContextualGovernanceOutcome.Subordinate,
                    Criticality = violation.Criticality,
                    OperationalContext = operationalContext,
                    Contexts = violation.Contexts,
                    ComparedInvariants = compared,
                    Reason =
                        $"{violation.Invariant} is subordinated to equal-priority invariant(s) " +
                        $"{string.Join(", ", strongerInvariants)} under HIG-C."
                };
            }

            return new ContextualGovernanceDecision
            {
                Outcome = ContextualGovernanceOutcome.Unresolved,
                Criticality = violation.Criticality,
                OperationalContext = operationalContext,
                Contexts = violation.Contexts,
                ComparedInvariants = compared,
                Reason =
                    $"{violation.Invariant} has no HIG-C distinction from equal-priority invariant(s) " +
                    $"{string.Join(", ", compared)}."
            };
        }

        private static int ContextualScore(
            InvariantViolation violation,
            OperationalContext operationalContext)
        {
            var score = (int)violation.Criticality * 10;
            return violation.Contexts.Contains(operationalContext)
                ? score + 1
                : score;
        }

        private static IReadOnlyList<IMetaInvariant> DefaultMetaInvariants()
        {
            return new IMetaInvariant[]
            {
                new PriorityMetaInvariant(),
                new OverrideSeverityMetaInvariant(),
                new JustificationMetaInvariant(),
                new AuditMetaInvariant(),
                new ReviewMetaInvariant()
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

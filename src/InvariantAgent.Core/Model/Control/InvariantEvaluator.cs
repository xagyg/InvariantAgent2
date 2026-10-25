using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Control.MetaInvariants;
using InvariantAgent.Core.Model.Control;
using InvariantAgent.Core.Model.Transition;
using System.Collections.Generic;
using System.Linq;

namespace InvariantAgent.Core.Control
{
    /// <summary>
    /// Default <see cref="IInvariantEvaluator"/> implementing invariant enforcement with
    /// Hierarchical Invariant Governance (HIG) and its context-aware extension (HIG-C).
    /// </summary>
    /// <remarks>
    /// <para>For a given scope the evaluator:</para>
    /// <list type="number">
    /// <item>Evaluates every invariant whose scope matches (plus <c>Transition</c>-scoped invariants) and records an Invariant event for each.</item>
    /// <item>Collects failures as <see cref="InvariantViolation"/>s.</item>
    /// <item>For each violation, applies HIG-C to same-layer conflicts; a violation that HIG-C preserves stays blocking.</item>
    /// <item>Otherwise asks every meta-invariant; only if all allow is an auditable <see cref="InvariantOverride"/> created.</item>
    /// <item>Returns the remaining (unresolved) violations together with the overrides.</item>
    /// </list>
    /// </remarks>
    public sealed class InvariantEvaluator : IInvariantEvaluator
    {
        // Object-level invariants (behavioural constraints).
        private readonly IReadOnlyList<IInvariant> _invariants;
        // Meta-invariants governing whether a violation may be overridden.
        private readonly IReadOnlyList<IMetaInvariant> _metaInvariants;

        // Pairs a meta-invariant with its result so metadata can be harvested afterwards.
        private sealed class MetaEvaluation
        {
            public IMetaInvariant MetaInvariant { get; init; } = default!;

            public MetaInvariantResult Result { get; init; } = default!;
        }

        /// <summary>Creates an evaluator using the default meta-invariant set.</summary>
        /// <param name="invariants">Invariants to enforce.</param>
        public InvariantEvaluator(IEnumerable<IInvariant> invariants)
            : this(invariants, DefaultMetaInvariants())
        {
        }

        /// <summary>Creates an evaluator with an explicit meta-invariant set.</summary>
        /// <param name="invariants">Invariants to enforce.</param>
        /// <param name="metaInvariants">Meta-invariants governing overrides.</param>
        public InvariantEvaluator(
            IEnumerable<IInvariant> invariants,
            IEnumerable<IMetaInvariant> metaInvariants)
        {
            _invariants = invariants.ToList();
            _metaInvariants = metaInvariants.ToList();
        }

        /// <inheritdoc />
        public InvariantEvaluationReport Evaluate(TransitionContext context, InvariantScope scope)
        {
            // 1. Evaluate applicable invariants and record an auditable event for each result.
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

            // 2. Convert failures into violations carrying the invariant's governance metadata.
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

            // 3. Resolve which violations may be overridden; everything else stays blocking.
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

        /// <summary>
        /// Determines which violations can be overridden under HIG/HIG-C and records a Control event
        /// for every preservation or authorised override.
        /// </summary>
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

                // HIG-C: this violation outranks its same-layer peers, so it must not be overridden.
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

                // All meta-invariants must allow the override (priority, severity, justification, audit, review).
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

                // Harvest metadata contributed by individual meta-invariants for the override record.
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

        /// <summary>
        /// HIG-C: compares a violation with other violations in the same layer using criticality and
        /// operational context, producing NotApplicable, Preserve, Subordinate or Unresolved.
        /// </summary>
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

        /// <summary>
        /// HIG-C score: criticality dominates (x10); a matching operational context adds +1 so context
        /// only breaks ties between equal criticalities.
        /// </summary>
        private static int ContextualScore(
            InvariantViolation violation,
            OperationalContext operationalContext)
        {
            var score = (int)violation.Criticality * 10;
            return violation.Contexts.Contains(operationalContext)
                ? score + 1
                : score;
        }

        /// <summary>The standard HIG meta-invariant set.</summary>
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

        /// <summary>Returns the first metadata value of type <typeparamref name="T"/> found under <paramref name="key"/>.</summary>
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

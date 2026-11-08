using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Control.MetaInvariants;
using InvariantAgent.Core.Model.Agent;
using InvariantAgent.Core.Model.Control;
using InvariantAgent.Core.Model.Transition;
using System;
using System.Collections.Generic;
using System.Linq;

namespace InvariantAgent.Core.Control
{
    /// <summary>
    /// Default <see cref="IInvariantEvaluator"/>: Hierarchical and Context-Aware Invariant Governance (HIG-C, IBAAM 3
    /// revised edition).
    /// </summary>
    /// <remarks>
    /// <para>For each checkpoint the evaluator:</para>
    /// <list type="number">
    /// <item>Builds the candidate set 𝒜: the action in flight, any planner alternatives (plan checkpoint only) and the
    /// null action a₀ (decline, hold or defer).</item>
    /// <item>Evaluates every applicable invariant for each candidate (<see cref="IInvariant.Evaluate"/> for actions,
    /// <see cref="IInvariant.EvaluateDecline"/> for a₀), giving violation degrees v(a, κ).</item>
    /// <item>Builds each candidate's <see cref="ViolationProfile"/> from its violation degrees, grouped by level and by
    /// criticality in the current context, C(I, κ).</item>
    /// <item>Takes the governed set R(𝒜, κ): the candidates whose profiles are lexicographically minimal.</item>
    /// <item>Walks R in the adaptive layer's order of preference (proposal, alternatives, decline) and selects the first
    /// candidate whose overrides every meta-invariant authorises (witnessed, non-fundamental unless pre-authorised,
    /// severity policy, audit, review).</item>
    /// <item>If no governed candidate is valid, or the least-bad outcome violates a fundamental invariant without a
    /// pre-authorised protocol, the runtime holds at a₀; in the latter case the decision is escalated.</item>
    /// </list>
    /// <para>
    /// Without conflict this reduces exactly to ordinary enforcement (Proposition 1): an action that satisfies every
    /// invariant has the zero profile and is selected; one that violates anything loses to declining unless declining is
    /// itself worse.
    /// </para>
    /// </remarks>
    public sealed class InvariantEvaluator : IInvariantEvaluator
    {
        private readonly IReadOnlyList<IInvariant> _invariants;
        private readonly IReadOnlyList<IMetaInvariant> _metaInvariants;
        private readonly IReadOnlyList<IEscalationProtocol> _protocols;

        // A candidate under evaluation: its records, violations and profile.
        private sealed class Candidate
        {
            public string Id { get; init; } = GovernanceCandidate.ProposedId;
            public AgentAction Action { get; init; }
            public bool IsDecline => Id == GovernanceCandidate.DeclineId;
            public List<InvariantEvaluationRecord> Records { get; } = new();
            public List<InvariantViolation> Violations { get; } = new();
            public ViolationProfile Profile { get; } = new();
            public bool Governed { get; set; }
            public bool Valid { get; set; }
            public List<string> Rejections { get; } = new();

            public double DegreeOf(string invariant) =>
                Violations.FirstOrDefault(v => v.Invariant == invariant)?.Degree ?? 0;
        }

        // Pairs a meta-invariant with its result so metadata can be harvested afterwards.
        private sealed class MetaEvaluation
        {
            public IMetaInvariant MetaInvariant { get; init; } = default!;

            public MetaInvariantResult Result { get; init; } = default!;
        }

        /// <summary>Creates an evaluator using the default meta-invariant set and no escalation protocols.</summary>
        /// <param name="invariants">Invariants to enforce.</param>
        public InvariantEvaluator(IEnumerable<IInvariant> invariants)
            : this(invariants, DefaultMetaInvariants())
        {
        }

        /// <summary>Creates an evaluator with an explicit meta-invariant set and no escalation protocols.</summary>
        /// <param name="invariants">Invariants to enforce.</param>
        /// <param name="metaInvariants">Meta-invariants governing overrides.</param>
        public InvariantEvaluator(
            IEnumerable<IInvariant> invariants,
            IEnumerable<IMetaInvariant> metaInvariants)
            : this(invariants, metaInvariants, Array.Empty<IEscalationProtocol>())
        {
        }

        /// <summary>Creates an evaluator with explicit meta-invariants and pre-authorised escalation protocols.</summary>
        /// <param name="invariants">Invariants to enforce.</param>
        /// <param name="metaInvariants">Meta-invariants governing overrides.</param>
        /// <param name="protocols">Pre-authorised protocols for fundamental-level conflicts (Section 7.2).</param>
        /// <exception cref="InvalidOperationException">
        /// The hierarchy breaks level–criticality consistency: an invariant below the fundamental level can be violated
        /// catastrophically in some context (Section 7.3).
        /// </exception>
        public InvariantEvaluator(
            IEnumerable<IInvariant> invariants,
            IEnumerable<IMetaInvariant> metaInvariants,
            IEnumerable<IEscalationProtocol> protocols)
        {
            _invariants = invariants.ToList();
            _metaInvariants = metaInvariants.ToList();
            _protocols = (protocols ?? Array.Empty<IEscalationProtocol>()).ToList();

            var problems = HierarchyConsistency.Check(_invariants);

            if (problems.Count > 0)
            {
                throw new InvalidOperationException(
                    "Invariant hierarchy fails level–criticality consistency: " + string.Join(" ", problems));
            }
        }

        /// <inheritdoc />
        public InvariantEvaluationReport Evaluate(TransitionContext context, InvariantScope scope)
        {
            return Evaluate(context, scope, Array.Empty<AgentAction>());
        }

        /// <inheritdoc />
        public InvariantEvaluationReport Evaluate(
            TransitionContext context,
            InvariantScope scope,
            IReadOnlyList<AgentAction> alternatives)
        {
            var operationalContext = context.OperationalContext;
            var applicable = _invariants
                .Where(i => i.Scope == scope || i.Scope == InvariantScope.Transition)
                .ToList();

            // 1. Candidate set: the action in flight, planner alternatives (plan checkpoint only), then the null action.
            var candidates = new List<Candidate>
            {
                EvaluateAction(context, GovernanceCandidate.ProposedId, context.Transition.ProposedAction, applicable)
            };

            if (scope == InvariantScope.Plan && alternatives != null)
            {
                var original = context.Transition.ProposedAction;

                try
                {
                    for (var i = 0; i < alternatives.Count; i++)
                    {
                        if (alternatives[i] == null)
                        {
                            continue;
                        }

                        context.Transition.ProposedAction = alternatives[i];
                        candidates.Add(EvaluateAction(context, $"alternative-{i + 1}", alternatives[i], applicable));
                    }
                }
                finally
                {
                    context.Transition.ProposedAction = original;
                }
            }

            var decline = EvaluateDecline(context, applicable);
            candidates.Add(decline);

            // 2. Governed set R(𝒜, κ): lexicographically minimal profiles.
            var minimal = candidates.Select(c => c.Profile).Min()!;

            foreach (var c in candidates)
            {
                c.Governed = c.Profile.CompareTo(minimal) == 0;
            }

            var governed = candidates.Where(c => c.Governed).ToList();
            var conflict = !candidates.Any(c => c.Profile.IsZero);

            // 3. Select within R in adaptive-preference order; the first candidate whose overrides are all authorised wins.
            Candidate selected = null;
            IReadOnlyList<InvariantOverride> overrides = Array.Empty<InvariantOverride>();
            var protocolUsed = "";

            foreach (var candidate in governed)
            {
                if (candidate.IsDecline)
                {
                    candidate.Valid = true;
                    selected = candidate;
                    break;
                }

                var protocol = candidate.Profile.HasFundamentalViolation
                    ? FindProtocol(context, scope, candidate)
                    : "";

                var authorised = Authorise(context, candidate, candidates, governed.Count, protocol);

                if (authorised != null)
                {
                    candidate.Valid = true;
                    selected = candidate;
                    overrides = authorised;
                    protocolUsed = protocol;
                    break;
                }
            }

            // No governed candidate could be authorised: hold at the null action.
            var heldWithoutValidCandidate = selected == null;
            selected ??= decline;

            // 4. Escalation: the least-bad outcome violates a fundamental invariant and no protocol covered it.
            var requiresEscalation = minimal.HasFundamentalViolation && protocolUsed.Length == 0;

            var reason = DescribeSelection(selected, governed, conflict, requiresEscalation, heldWithoutValidCandidate, protocolUsed);

            var resolution = new GovernanceResolution
            {
                Scope = scope,
                OperationalContext = operationalContext,
                Candidates = candidates.Select(ToSummary).ToList(),
                GovernedSet = governed.Select(c => c.Id).ToList(),
                SelectedId = selected.Id,
                Conflict = conflict,
                RequiresEscalation = requiresEscalation,
                EscalationProtocol = protocolUsed,
                Reason = reason
            };

            // 5. Record the decision. Invariant events describe the action the transition carries forward (or the
            // proposal that was declined), so the event trail and replay keep their meaning.
            var subject = selected.IsDecline ? candidates[0] : selected;
            RecordInvariantEvents(context, subject, operationalContext);
            RecordResolutionEvents(context, resolution, candidates, selected, overrides, alternatives?.Count > 0);

            if (selected.IsDecline)
            {
                return new InvariantEvaluationReport
                {
                    Scope = scope,
                    Violations = candidates[0].Violations.Count > 0
                        ? candidates[0].Violations
                        : governed.Where(c => !c.IsDecline).SelectMany(c => c.Violations).ToList(),
                    Declined = true,
                    RequiresEscalation = requiresEscalation,
                    Resolution = resolution
                };
            }

            return new InvariantEvaluationReport
            {
                Scope = scope,
                Overrides = overrides,
                SelectedAlternative = selected.Id == GovernanceCandidate.ProposedId ? null : selected.Action,
                Resolution = resolution
            };
        }

        // Evaluates every applicable invariant against the action currently set on the transition.
        private static Candidate EvaluateAction(
            TransitionContext context,
            string id,
            AgentAction action,
            IReadOnlyList<IInvariant> applicable)
        {
            var candidate = new Candidate { Id = id, Action = action };

            foreach (var invariant in applicable)
            {
                Add(candidate, invariant, invariant.Evaluate(context), context.OperationalContext);
            }

            return candidate;
        }

        // Evaluates every applicable invariant against the null action a₀.
        private static Candidate EvaluateDecline(TransitionContext context, IReadOnlyList<IInvariant> applicable)
        {
            var candidate = new Candidate { Id = GovernanceCandidate.DeclineId };

            foreach (var invariant in applicable)
            {
                Add(candidate, invariant, invariant.EvaluateDecline(context), context.OperationalContext);
            }

            return candidate;
        }

        private static void Add(
            Candidate candidate,
            IInvariant invariant,
            InvariantResult result,
            OperationalContext operationalContext)
        {
            candidate.Records.Add(new InvariantEvaluationRecord
            {
                Invariant = invariant,
                Result = result
            });

            if (result.Passed)
            {
                return;
            }

            var criticality = invariant.CriticalityIn(operationalContext);

            candidate.Violations.Add(new InvariantViolation
            {
                Invariant = invariant.Name,
                Category = invariant.Category,
                Scope = invariant.Scope,
                Severity = invariant.Severity,
                Layer = invariant.Layer,
                Criticality = invariant.Criticality,
                CriticalityInContext = criticality,
                Degree = result.Degree,
                Contexts = invariant.Contexts,
                Reason = result.Reason
            });

            candidate.Profile.Add(invariant.Layer, criticality, result.Degree);
        }

        // The first registered protocol that authorises a candidate's fundamental-level violations, or "".
        private string FindProtocol(TransitionContext context, InvariantScope scope, Candidate candidate)
        {
            if (_protocols.Count == 0)
            {
                return "";
            }

            var request = new EscalationRequest
            {
                Context = context,
                Scope = scope,
                Action = candidate.Action,
                FundamentalViolations = candidate.Violations
                    .Where(v => v.Layer == InvariantLayer.Fundamental)
                    .ToList(),
                Profile = candidate.Profile
            };

            return _protocols.FirstOrDefault(p => p.Authorises(request))?.Name ?? "";
        }

        /// <summary>
        /// Builds a witnessed override for every violation of <paramref name="candidate"/> and asks every meta-invariant to
        /// allow it. Returns the overrides, or <c>null</c> if any meta-invariant rejects any of them.
        /// </summary>
        private IReadOnlyList<InvariantOverride> Authorise(
            TransitionContext context,
            Candidate candidate,
            IReadOnlyList<Candidate> candidates,
            int governedSetSize,
            string protocol)
        {
            var overrides = new List<InvariantOverride>();

            foreach (var violation in candidate.Violations)
            {
                var witnesses = WitnessesFor(candidate, violation, candidates);

                var metaContext = new MetaInvariantContext
                {
                    TransitionContext = context,
                    CandidateViolation = violation,
                    AllViolations = candidate.Violations,
                    EvaluationRecords = candidate.Records,
                    CandidateId = candidate.Id,
                    CandidateProfile = candidate.Profile,
                    Witnesses = witnesses,
                    GovernedSetSize = governedSetSize,
                    OperationalContext = context.OperationalContext,
                    EscalationProtocol = protocol
                };

                var metaEvaluations = _metaInvariants
                    .Select(m => new MetaEvaluation
                    {
                        MetaInvariant = m,
                        Result = m.Evaluate(metaContext)
                    })
                    .ToList();

                var rejected = metaEvaluations.Where(e => !e.Result.Passed).ToList();

                if (rejected.Count > 0)
                {
                    candidate.Rejections.AddRange(rejected.Select(e =>
                        $"{e.MetaInvariant.Name}: {e.Result.Reason}"));
                    return null;
                }

                var preserved = candidate.Records
                    .Where(r => r.Result.Passed && r.Invariant.Layer > violation.Layer)
                    .Select(r => r.Invariant.Name)
                    .ToArray();
                var justification =
                    GetMetadata<string>(metaEvaluations, "Justification")
                    ?? $"{violation.Invariant} was overridden: " + string.Join("; ", witnesses);
                var requiresReview = GetMetadata<bool>(metaEvaluations, "RequiresReview");
                var reviewReasons =
                    GetMetadata<string[]>(metaEvaluations, "ReviewReasons")
                    ?? Array.Empty<string>();
                var categories = metaEvaluations
                    .Select(e => e.MetaInvariant.Category)
                    .Distinct()
                    .ToArray();

                overrides.Add(new InvariantOverride
                {
                    OverriddenViolation = violation,
                    Witnesses = witnesses,
                    PreservedHigherPriorityInvariants = preserved,
                    Justification = justification,
                    MetaInvariantCategory = string.Join(", ", categories),
                    MetaInvariantCategories = categories,
                    RequiresReview = requiresReview,
                    ReviewReasons = reviewReasons,
                    ContextualDecision = ContextualDecisionFor(violation, witnesses, context.OperationalContext),
                    EscalationProtocol = violation.Layer == InvariantLayer.Fundamental ? protocol : ""
                });
            }

            return overrides;
        }

        /// <summary>
        /// Witnesses for overriding <paramref name="violation"/> (Section 7.1): every other candidate that violates the
        /// same invariant less, with the profile position at which it is worse than <paramref name="candidate"/>.
        /// </summary>
        private static IReadOnlyList<OverrideWitness> WitnessesFor(
            Candidate candidate,
            InvariantViolation violation,
            IReadOnlyList<Candidate> candidates)
        {
            var witnesses = new List<OverrideWitness>();

            foreach (var other in candidates)
            {
                if (ReferenceEquals(other, candidate))
                {
                    continue;
                }

                var competingDegree = other.DegreeOf(violation.Invariant);

                if (competingDegree >= violation.Degree)
                {
                    continue;
                }

                var position = other.Profile.DecidingPosition(candidate.Profile);
                var worse = other.Profile.CompareTo(candidate.Profile) > 0;

                witnesses.Add(new OverrideWitness
                {
                    Candidate = other.Id,
                    CompetingDegree = competingDegree,
                    DecidingLayer = position?.Layer ?? violation.Layer,
                    DecidingCriticality = position?.Criticality ?? violation.CriticalityInContext,
                    DecidingInvariants = position == null
                        ? Array.Empty<string>()
                        : other.Violations
                            .Where(v => v.Layer == position.Value.Layer &&
                                        v.CriticalityInContext == position.Value.Criticality)
                            .Select(v => v.Invariant)
                            .ToArray(),
                    Worse = worse
                });
            }

            return witnesses;
        }

        // Same-level comparisons are decided by criticality in context; cross-level ones by priority.
        private static ContextualGovernanceDecision ContextualDecisionFor(
            InvariantViolation violation,
            IReadOnlyList<OverrideWitness> witnesses,
            OperationalContext operationalContext)
        {
            var sameLevel = witnesses.Where(w => w.Worse && w.DecidingLayer == violation.Layer).ToList();

            return new ContextualGovernanceDecision
            {
                Outcome = sameLevel.Count > 0
                    ? ContextualGovernanceOutcome.Subordinate
                    : ContextualGovernanceOutcome.NotApplicable,
                Criticality = violation.CriticalityInContext,
                OperationalContext = operationalContext,
                Contexts = violation.Contexts,
                ComparedInvariants = witnesses
                    .SelectMany(w => w.DecidingInvariants)
                    .Distinct()
                    .ToArray(),
                Reason = sameLevel.Count > 0
                    ? $"{violation.Invariant} ({violation.CriticalityInContext} in {operationalContext}) is subordinated within " +
                      $"level {violation.Layer}: " + string.Join("; ", sameLevel)
                    : witnesses.Count == 0
                        ? $"No candidate violates {violation.Invariant} less."
                        : $"{violation.Invariant} yields to higher-level obligations: " + string.Join("; ", witnesses)
            };
        }

        private static string DescribeSelection(
            Candidate selected,
            IReadOnlyList<Candidate> governed,
            bool conflict,
            bool requiresEscalation,
            bool heldWithoutValidCandidate,
            string protocol)
        {
            if (requiresEscalation)
            {
                return "Every candidate violates a fundamental invariant; holding and escalating for human review.";
            }

            if (heldWithoutValidCandidate)
            {
                return "No governed candidate could be authorised by the meta-invariants; holding at decline.";
            }

            if (selected.IsDecline)
            {
                return conflict
                    ? "Declining has the least-bad violation profile."
                    : "Declining satisfies every invariant; the proposal does not.";
            }

            if (!conflict)
            {
                return $"{selected.Id} satisfies every invariant.";
            }

            var text = $"{selected.Id} has the least-bad violation profile ({selected.Profile})";

            if (governed.Count > 1)
            {
                text += $"; tied with {string.Join(", ", governed.Where(g => g != selected).Select(g => g.Id))}, " +
                        "chosen by adaptive-layer preference";
            }

            if (protocol.Length > 0)
            {
                text += $"; fundamental violation authorised by protocol '{protocol}'";
            }

            return text + ".";
        }

        private static GovernanceCandidate ToSummary(Candidate c)
        {
            return new GovernanceCandidate
            {
                Id = c.Id,
                Action = c.Action,
                Profile = c.Profile.ToString(),
                Violations = c.Violations,
                Governed = c.Governed,
                Valid = c.Valid,
                Rejections = c.Rejections
            };
        }

        // One Invariant event per invariant evaluated for the action the transition carries (unchanged format).
        private static void RecordInvariantEvents(
            TransitionContext context,
            Candidate subject,
            OperationalContext operationalContext)
        {
            foreach (var record in subject.Records)
            {
                var invariant = record.Invariant;
                var result = record.Result;

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
                        ["CriticalityInContext"] = invariant.CriticalityIn(operationalContext).ToString(),
                        ["Contexts"] = invariant.Contexts.Select(c => c.ToString()).ToArray(),
                        ["OperationalContext"] = operationalContext.ToString(),
                        ["Passed"] = result.Passed,
                        ["Degree"] = result.Degree,
                        ["Reason"] = result.Reason
                    });
            }
        }

        // Control events: the resolution record (when there was anything to resolve), each override and any escalation.
        private static void RecordResolutionEvents(
            TransitionContext context,
            GovernanceResolution resolution,
            IReadOnlyList<Candidate> candidates,
            Candidate selected,
            IReadOnlyList<InvariantOverride> overrides,
            bool hadAlternatives)
        {
            var transition = context.Transition;
            var anyViolation = candidates.Any(c => c.Violations.Count > 0 && !c.IsDecline);

            if (resolution.Conflict || hadAlternatives || (anyViolation && !selected.IsDecline))
            {
                transition.AddEvent(
                    TransitionEventStage.Control,
                    $"HIG-C resolution: selected {resolution.SelectedId}",
                    new Dictionary<string, object>
                    {
                        ["Scope"] = resolution.Scope.ToString(),
                        ["OperationalContext"] = resolution.OperationalContext.ToString(),
                        ["Conflict"] = resolution.Conflict,
                        ["Candidates"] = resolution.Candidates
                            .Select(c => new
                            {
                                c.Id,
                                Capability = c.Action?.Capability,
                                c.Profile,
                                c.Governed,
                                c.Valid,
                                c.Rejections
                            })
                            .ToArray(),
                        ["GovernedSet"] = resolution.GovernedSet.ToArray(),
                        ["Selected"] = resolution.SelectedId,
                        ["RequiresEscalation"] = resolution.RequiresEscalation,
                        ["EscalationProtocol"] = resolution.EscalationProtocol,
                        ["Reason"] = resolution.Reason
                    });
            }

            foreach (var overrideDecision in overrides)
            {
                var violation = overrideDecision.OverriddenViolation;

                transition.AddEvent(
                    TransitionEventStage.Control,
                    $"Override authorised: {violation.Invariant}",
                    new Dictionary<string, object>
                    {
                        ["MetaInvariantCategory"] = overrideDecision.MetaInvariantCategory,
                        ["OverriddenInvariant"] = violation.Invariant,
                        ["OverriddenLayer"] = violation.Layer.ToString(),
                        ["Criticality"] = violation.CriticalityInContext.ToString(),
                        ["Degree"] = violation.Degree,
                        ["OperationalContext"] = context.OperationalContext.ToString(),
                        ["SelectedCandidate"] = selected.Id,
                        ["ContextualGovernanceOutcome"] = overrideDecision.ContextualDecision.Outcome.ToString(),
                        ["ContextualGovernanceReason"] = overrideDecision.ContextualDecision.Reason,
                        ["ComparedInvariants"] = overrideDecision.ContextualDecision.ComparedInvariants,
                        ["Witnesses"] = overrideDecision.Witnesses.Select(w => w.ToString()).ToArray(),
                        ["PreservedHigherPriorityInvariants"] = overrideDecision.PreservedHigherPriorityInvariants,
                        ["Justification"] = overrideDecision.Justification,
                        ["Audit"] = overrideDecision.MetaInvariantCategories.Contains(MetaInvariantCategory.Audit),
                        ["RequiresReview"] = overrideDecision.RequiresReview,
                        ["ReviewReasons"] = overrideDecision.ReviewReasons,
                        ["EscalationProtocol"] = overrideDecision.EscalationProtocol
                    });
            }

            if (resolution.RequiresEscalation)
            {
                transition.AddEvent(
                    TransitionEventStage.Control,
                    "Escalation required: fundamental conflict held for human review",
                    new Dictionary<string, object>
                    {
                        ["Scope"] = resolution.Scope.ToString(),
                        ["RequiresReview"] = true,
                        ["ReviewReasons"] = new[] { resolution.Reason },
                        ["Recommendation"] = resolution.GovernedSet.ToArray(),
                        ["Reason"] = resolution.Reason
                    });
            }
            else if (selected.IsDecline && candidates.Any(c => !c.IsDecline && c.Governed))
            {
                // A governed action existed but could not be authorised: worth a reviewer's attention.
                transition.AddEvent(
                    TransitionEventStage.Control,
                    "HIG-C held at decline: no governed candidate was authorised",
                    new Dictionary<string, object>
                    {
                        ["Scope"] = resolution.Scope.ToString(),
                        ["RequiresReview"] = true,
                        ["ReviewReasons"] = candidates
                            .Where(c => c.Governed && !c.IsDecline)
                            .SelectMany(c => c.Rejections)
                            .ToArray(),
                        ["Reason"] = resolution.Reason
                    });
            }
        }

        /// <summary>The standard HIG-C meta-invariant set.</summary>
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

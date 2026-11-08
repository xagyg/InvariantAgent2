using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Control;
using InvariantAgent.Core.Model.Agent;
using InvariantAgent.Core.Model.Control;
using InvariantAgent.Core.Model.Transition;
using InvariantAgent.Safety.Invariants.Mission;
using Xunit;

namespace InvariantAgent.Tests.Governance;

/// <summary>
/// Tests for Hierarchical and Context-Aware Invariant Governance (HIG-C, IBAAM 3 revised edition) in
/// <c>InvariantEvaluator</c>: the lexicographic resolution rule over candidate actions (including declining), witnessed
/// overrides, fundamental-level escalation, context-dependent criticality, level–criticality consistency, and the four
/// worked examples of Section 8.
/// </summary>
public sealed class HierarchicalInvariantGovernanceTests
{
    // ---------------------------------------------------------------------------------------------------------------
    // Basic properties (Section 6.3)
    // ---------------------------------------------------------------------------------------------------------------

    [Fact]
    public void Evaluate_WhenNoConflict_ReducesToEnforcement()
    {
        // Proposition 1: an action that satisfies every invariant has the zero profile and is selected unchanged.
        var evaluator = new InvariantEvaluator(new IInvariant[]
        {
            TestInvariant.Pass("SystemIntegrity", InvariantLayer.Fundamental),
            new ServiceObligationInvariant()
        });

        var report = evaluator.Evaluate(NewContext(), InvariantScope.Plan);

        Assert.True(report.Passed);
        Assert.Empty(report.Overrides);
        Assert.False(report.Declined);
        Assert.False(report.Resolution.Conflict);
        Assert.Equal(GovernanceCandidate.ProposedId, report.Resolution.SelectedId);
        Assert.Null(ConflictRecord.From(report));
    }

    [Fact]
    public void Evaluate_WhenDecliningIsFree_AnyViolationDeclines()
    {
        // Without an invariant that declining violates, the null action has the zero profile, so it beats any action
        // that violates anything: pure enforcement, no overrides.
        var evaluator = new InvariantEvaluator(new IInvariant[]
        {
            TestInvariant.Pass("SystemIntegrity", InvariantLayer.Fundamental),
            TestInvariant.Fail("PersonalisationPreference", InvariantLayer.AdaptiveHeuristic, InvariantSeverity.Warning)
        });

        var report = evaluator.Evaluate(NewContext(), InvariantScope.Plan);

        Assert.False(report.Passed);
        Assert.True(report.Declined);
        Assert.False(report.RequiresEscalation);
        Assert.Equal("PersonalisationPreference", Assert.Single(report.Violations).Invariant);
        Assert.Empty(report.Overrides);
    }

    [Fact]
    public void Evaluate_WhenLowerLevelViolationIsCheaperThanDeclining_AuthorisesWitnessedOverride()
    {
        // Section 8.1 shape: the action breaks only a heuristic; declining breaks the mission-level service obligation.
        var evaluator = new InvariantEvaluator(new IInvariant[]
        {
            TestInvariant.Pass("SystemIntegrity", InvariantLayer.Fundamental),
            TestInvariant.Fail("PersonalisationPreference", InvariantLayer.AdaptiveHeuristic, InvariantSeverity.Warning),
            new ServiceObligationInvariant()
        });
        var context = NewContext();

        var report = evaluator.Evaluate(context, InvariantScope.Plan);

        Assert.True(report.Passed);
        Assert.Empty(report.Violations);
        Assert.True(report.Resolution.Conflict);
        var overrideDecision = Assert.Single(report.Overrides);
        Assert.Equal("PersonalisationPreference", overrideDecision.OverriddenViolation.Invariant);
        Assert.Equal(InvariantLayer.AdaptiveHeuristic, overrideDecision.OverriddenLayer);

        // Witnessed: declining violates it less (not at all) but is worse at the mission level.
        var witness = Assert.Single(overrideDecision.Witnesses);
        Assert.Equal(GovernanceCandidate.DeclineId, witness.Candidate);
        Assert.True(witness.Worse);
        Assert.Equal(InvariantLayer.Mission, witness.DecidingLayer);
        Assert.Contains(nameof(ServiceObligationInvariant), witness.DecidingInvariants);
        Assert.Equal(ContextualGovernanceOutcome.NotApplicable, overrideDecision.ContextualDecision.Outcome);

        Assert.Contains("SystemIntegrity", overrideDecision.PreservedHigherPriorityInvariants);
        Assert.Contains(MetaInvariantCategory.Priority, overrideDecision.MetaInvariantCategories);
        Assert.Contains(MetaInvariantCategory.OverrideSeverity, overrideDecision.MetaInvariantCategories);
        Assert.Contains(MetaInvariantCategory.Justification, overrideDecision.MetaInvariantCategories);
        Assert.Contains(MetaInvariantCategory.Audit, overrideDecision.MetaInvariantCategories);
        Assert.Contains(MetaInvariantCategory.Review, overrideDecision.MetaInvariantCategories);
        Assert.Contains("worse", overrideDecision.Justification);
        Assert.False(overrideDecision.RequiresReview);
        Assert.Contains(
            context.Transition.Events,
            e => e.Metadata.TryGetValue("Audit", out var audit) && audit is true);
    }

    [Fact]
    public void Evaluate_WhenAnAlternativeSatisfiesEverything_SelectsItWithoutOverride()
    {
        // The earlier rule overrode a soft violation whenever a higher-level invariant passed, even with no conflict.
        // Under HIG-C an alternative that satisfies everything has the zero profile and wins: no override at all.
        var evaluator = new InvariantEvaluator(new IInvariant[]
        {
            TestInvariant.Pass("SystemIntegrity", InvariantLayer.Fundamental),
            ByCapability.Of("PersonalisationPreference", InvariantLayer.AdaptiveHeuristic,
                InvariantCriticality.Medium, ("export-full", 1.0)),
            new ServiceObligationInvariant()
        });
        var context = NewContext(proposed: Action("export-full"));

        var report = evaluator.Evaluate(context, InvariantScope.Plan, new[] { Action("export-standard") });

        Assert.True(report.Passed);
        Assert.Empty(report.Overrides);
        Assert.Equal("export-standard", report.SelectedAlternative?.Capability);
        Assert.Equal("alternative-1", report.Resolution.SelectedId);
        Assert.False(report.Resolution.Conflict);
    }

    [Fact]
    public void Evaluate_WhenFundamentalInvariantFails_Declines()
    {
        // Proposition 3: declining satisfies every fundamental invariant, so no governed outcome violates one.
        var evaluator = new InvariantEvaluator(new IInvariant[]
        {
            TestInvariant.Fail("SystemIntegrity", InvariantLayer.Fundamental),
            TestInvariant.Pass("Transparency", InvariantLayer.Behavioural),
            new ServiceObligationInvariant()
        });

        var report = evaluator.Evaluate(NewContext(), InvariantScope.Plan);

        Assert.False(report.Passed);
        Assert.True(report.Declined);
        Assert.False(report.RequiresEscalation);
        Assert.Equal("SystemIntegrity", Assert.Single(report.Violations).Invariant);
        Assert.Empty(report.Overrides);
    }

    [Fact]
    public void Evaluate_WhenMissionViolationIsWorseThanDeclining_Declines()
    {
        // Strict between levels: the action's mission violation plus a heuristic violation is worse than declining's
        // mission violation alone.
        var evaluator = new InvariantEvaluator(new IInvariant[]
        {
            TestInvariant.Fail("MissionAccuracy", InvariantLayer.Mission, InvariantSeverity.Warning),
            TestInvariant.Fail("PersonalisationPreference", InvariantLayer.AdaptiveHeuristic, InvariantSeverity.Warning),
            TestInvariant.Pass("SystemIntegrity", InvariantLayer.Fundamental),
            new ServiceObligationInvariant()
        });

        var report = evaluator.Evaluate(NewContext(), InvariantScope.Plan);

        Assert.False(report.Passed);
        Assert.True(report.Declined);
        Assert.Contains(report.Violations, v => v.Invariant == "MissionAccuracy");
        Assert.Contains(report.Violations, v => v.Invariant == "PersonalisationPreference");
        Assert.Empty(report.Overrides);
    }

    [Fact]
    public void Evaluate_WhenGovernedOverrideIsHardSeverity_HoldsAtDecline()
    {
        // Deployment policy: Error-severity violations are never overridden, so the governed action is not valid.
        var evaluator = new InvariantEvaluator(new IInvariant[]
        {
            TestInvariant.Fail("HelpfulTone", InvariantLayer.Behavioural, InvariantSeverity.Error),
            new ServiceObligationInvariant()
        });
        var context = NewContext();

        var report = evaluator.Evaluate(context, InvariantScope.Plan);

        Assert.False(report.Passed);
        Assert.True(report.Declined);
        var proposed = report.Resolution.Candidates.Single(c => c.Id == GovernanceCandidate.ProposedId);
        Assert.True(proposed.Governed);
        Assert.False(proposed.Valid);
        Assert.Contains(proposed.Rejections, r => r.Contains(nameof(Core.Control.MetaInvariants.OverrideSeverityMetaInvariant)));
        Assert.Contains(context.Transition.Events, e => e.Message.StartsWith("HIG-C held at decline"));
    }

    [Fact]
    public void Evaluate_WhenSameLevelViolationsHaveEqualCriticality_ProceedsAndFlagsReviewOfCount()
    {
        // Two behavioural soft violations are still cheaper than leaving the request unanswered (mission level).
        var evaluator = new InvariantEvaluator(
            new IInvariant[]
            {
                TestInvariant.Pass("SystemIntegrity", InvariantLayer.Fundamental),
                TestInvariant.Fail("HelpfulTone", InvariantLayer.Behavioural, InvariantSeverity.Warning),
                TestInvariant.Fail("ConciseStyle", InvariantLayer.Behavioural, InvariantSeverity.Warning),
                new ServiceObligationInvariant()
            },
            new IMetaInvariant[]
            {
                new Core.Control.MetaInvariants.PriorityMetaInvariant(),
                new Core.Control.MetaInvariants.JustificationMetaInvariant(),
                new Core.Control.MetaInvariants.ReviewMetaInvariant(violationReviewThreshold: 2)
            });

        var report = evaluator.Evaluate(NewContext(), InvariantScope.Plan);

        Assert.True(report.Passed);
        Assert.Equal(2, report.Overrides.Count);
        Assert.All(report.Overrides, o => Assert.True(o.RequiresReview));
    }

    [Fact]
    public void Evaluate_WhenCandidatesTie_AdaptivePreferenceDecidesAndReviewIsFlagged()
    {
        var evaluator = new InvariantEvaluator(new IInvariant[]
        {
            ByCapability.Of("ResponseLatency", InvariantLayer.Mission, InvariantCriticality.Low,
                ("full", 1.0), ("defer", 1.0)),
            new ServiceObligationInvariant()
        });
        var context = NewContext(proposed: Action("full"));

        var report = evaluator.Evaluate(context, InvariantScope.Plan, new[] { Action("defer") });

        Assert.True(report.Passed);
        Assert.Null(report.SelectedAlternative);
        Assert.Equal(new[] { "proposed", "alternative-1" }, report.Resolution.GovernedSet);
        var overrideDecision = Assert.Single(report.Overrides);
        Assert.True(overrideDecision.RequiresReview);
        Assert.Contains(overrideDecision.ReviewReasons, r => r.Contains("tie"));
    }

    [Fact]
    public void Evaluate_WhenCustomMetaInvariantRejects_Declines()
    {
        var evaluator = new InvariantEvaluator(
            new IInvariant[]
            {
                TestInvariant.Pass("SystemIntegrity", InvariantLayer.Fundamental),
                TestInvariant.Fail("PersonalisationPreference", InvariantLayer.AdaptiveHeuristic, InvariantSeverity.Warning),
                new ServiceObligationInvariant()
            },
            new IMetaInvariant[]
            {
                new RejectingMetaInvariant()
            });

        var report = evaluator.Evaluate(NewContext(), InvariantScope.Plan);

        Assert.False(report.Passed);
        Assert.True(report.Declined);
        Assert.Single(report.Violations);
        Assert.Empty(report.Overrides);
    }

    [Fact]
    public void Constructor_WhenNonFundamentalInvariantCanBeCatastrophic_Throws()
    {
        // Level–criticality consistency (Section 7.3).
        var ex = Assert.Throws<InvalidOperationException>(() => new InvariantEvaluator(new IInvariant[]
        {
            ByCapability.Of("CoverageTarget", InvariantLayer.Mission, InvariantCriticality.Catastrophic)
        }));

        Assert.Contains("CoverageTarget", ex.Message);
    }

    [Fact]
    public void CriticalityIn_DefaultRaise_StopsAtCriticalBelowFundamental()
    {
        IInvariant mission = TestInvariant.Fail(
            "Inspection", InvariantLayer.Mission, InvariantSeverity.Warning, InvariantCriticality.Critical,
            OperationalContext.Emergency);
        IInvariant fundamental = new ContextualInvariant("Safety", InvariantLayer.Fundamental,
            InvariantCriticality.Critical, OperationalContext.Emergency);

        Assert.Equal(InvariantCriticality.Critical, mission.CriticalityIn(OperationalContext.Emergency));
        Assert.Equal(InvariantCriticality.Catastrophic, fundamental.CriticalityIn(OperationalContext.Emergency));
        Assert.Equal(InvariantCriticality.Critical, fundamental.CriticalityIn(OperationalContext.Normal));
    }

    [Fact]
    public void ViolationProfile_ComparesLevelFirstThenCriticality()
    {
        var mission = new ViolationProfile();
        mission.Add(InvariantLayer.Mission, InvariantCriticality.Low, 0.01);

        var behavioural = new ViolationProfile();
        behavioural.Add(InvariantLayer.Behavioural, InvariantCriticality.Catastrophic, 1);

        var critical = new ViolationProfile();
        critical.Add(InvariantLayer.Mission, InvariantCriticality.Critical, 0.1);

        var high = new ViolationProfile();
        high.Add(InvariantLayer.Mission, InvariantCriticality.High, 1);

        Assert.True(mission.CompareTo(behavioural) > 0);
        Assert.True(critical.CompareTo(high) > 0);
        Assert.Equal(0, ViolationProfile.Zero.CompareTo(new ViolationProfile()));
        Assert.Equal("M/Critical=0.1", critical.ToString());
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Worked examples (Section 8)
    // ---------------------------------------------------------------------------------------------------------------

    [Fact]
    public void Example82_SameLevel_ContextReversesTheDecision()
    {
        // I_lat: 3 s latency budget. Low in Normal, High in Degraded. I_comp: completeness, Medium.
        // a1 full analysis (8 s, complete); a2 fast partial with caveat (1 s, 70%); a3 defer (complete, budget missed).
        IInvariant[] Invariants() => new IInvariant[]
        {
            new TableInvariant("ResponseLatency", InvariantLayer.Mission,
                new Dictionary<OperationalContext, InvariantCriticality>
                {
                    [OperationalContext.Normal] = InvariantCriticality.Low,
                    [OperationalContext.Degraded] = InvariantCriticality.High
                },
                ("full", 1.0), ("defer", 1.0)),
            ByCapability.Of("ResponseCompleteness", InvariantLayer.Mission, InvariantCriticality.Medium,
                ("partial", 0.3)),
            new ServiceObligationInvariant()
        };
        var alternatives = new[] { Action("partial"), Action("defer") };

        var normal = new InvariantEvaluator(Invariants())
            .Evaluate(NewContext(OperationalContext.Normal, Action("full")), InvariantScope.Plan, alternatives);
        var degraded = new InvariantEvaluator(Invariants())
            .Evaluate(NewContext(OperationalContext.Degraded, Action("full")), InvariantScope.Plan, alternatives);

        // Normal: a1 and a3 tie at M/Low=1; the adaptive layer prefers the proposal (a1).
        Assert.True(normal.Passed);
        Assert.Equal(new[] { "proposed", "alternative-2" }, normal.Resolution.GovernedSet);
        Assert.Null(normal.SelectedAlternative);
        Assert.Equal("ResponseLatency", Assert.Single(normal.Overrides).OverriddenViolation.Invariant);

        // Degraded: missing the budget is High, so the fast partial answer is the only governed action.
        Assert.True(degraded.Passed);
        Assert.Equal(new[] { "alternative-1" }, degraded.Resolution.GovernedSet);
        Assert.Equal("partial", degraded.SelectedAlternative?.Capability);
        var overrideDecision = Assert.Single(degraded.Overrides);
        Assert.Equal("ResponseCompleteness", overrideDecision.OverriddenViolation.Invariant);
        Assert.Equal(0.3, overrideDecision.OverriddenViolation.Degree, 3);
        Assert.Equal(ContextualGovernanceOutcome.Subordinate, overrideDecision.ContextualDecision.Outcome);
        Assert.Contains("ResponseLatency", overrideDecision.ContextualDecision.ComparedInvariants);
    }

    [Fact]
    public void Example83_ThresholdInvariants_FundamentalReserveAndContextDependentInspection()
    {
        // I_res (F): battery reserve. I_cov (M, threshold): coverage >= 80%. I_insp (M, conditional): inspect detections
        // with confidence >= 0.6 — Critical while a detection is pending (modelled as the Emergency context).
        // a1 continue search (85%, no inspection); a2 inspect then return (60%); a3 inspect then continue (reserve breached).
        IInvariant[] Invariants(double confidence) => new IInvariant[]
        {
            ByCapability.Of("BatteryReserve", InvariantLayer.Fundamental, InvariantCriticality.Catastrophic,
                ("inspect-continue", 1.0)),
            ByCapability.Of("SectorCoverage", InvariantLayer.Mission, InvariantCriticality.High,
                ("inspect-return", 0.25)),
            new ContextualInvariant("CloseInspection", InvariantLayer.Mission, InvariantCriticality.High,
                OperationalContext.Emergency,
                degree: a => confidence >= 0.6 && a?.Capability == "continue-search" ? 1.0 : 0.0),
            new ServiceObligationInvariant(InvariantCriticality.High)
        };
        var alternatives = new[] { Action("inspect-return"), Action("inspect-continue") };

        var detection = new InvariantEvaluator(Invariants(0.7))
            .Evaluate(NewContext(OperationalContext.Emergency, Action("continue-search")), InvariantScope.Plan, alternatives);
        var noDetection = new InvariantEvaluator(Invariants(0.3))
            .Evaluate(NewContext(OperationalContext.Emergency, Action("continue-search")), InvariantScope.Plan, alternatives);

        Assert.True(detection.Passed);
        Assert.Equal("inspect-return", detection.SelectedAlternative?.Capability);
        var inspectContinue = detection.Resolution.Candidates.Single(c => c.Action?.Capability == "inspect-continue");
        Assert.Contains("F/Catastrophic=1", inspectContinue.Profile);
        Assert.Equal("SectorCoverage", Assert.Single(detection.Overrides).OverriddenViolation.Invariant);

        // Below the confidence threshold the inspection invariant does not apply: continuing the search is clean.
        Assert.True(noDetection.Passed);
        Assert.Null(noDetection.SelectedAlternative);
        Assert.Empty(noDetection.Overrides);
    }

    [Fact]
    public void Example84_FundamentalConflict_WithoutProtocol_HoldsAndEscalates()
    {
        var evaluator = new InvariantEvaluator(ClinicalInvariants());
        var context = NewContext(OperationalContext.Emergency, Action("disclose-minimum"));

        var report = evaluator.Evaluate(context, InvariantScope.Plan, new[] { Action("disclose-full") });

        // The rule recommends minimum disclosure, but it violates a fundamental invariant: hold and escalate.
        Assert.False(report.Passed);
        Assert.True(report.Declined);
        Assert.True(report.RequiresEscalation);
        Assert.Equal(new[] { "proposed" }, report.Resolution.GovernedSet);
        Assert.Contains("PatientConfidentiality", report.Violations.Select(v => v.Invariant));
        Assert.Contains(context.Transition.Events, e => e.Message.StartsWith("Escalation required"));
        Assert.True(ConflictRecord.From(report)!.Escalated);
    }

    [Fact]
    public void Example84_FundamentalConflict_WithPreAuthorisedProtocol_ProceedsWithReview()
    {
        var evaluator = new InvariantEvaluator(
            ClinicalInvariants(),
            new IMetaInvariant[]
            {
                new Core.Control.MetaInvariants.PriorityMetaInvariant(),
                new Core.Control.MetaInvariants.OverrideSeverityMetaInvariant(),
                new Core.Control.MetaInvariants.JustificationMetaInvariant(),
                new Core.Control.MetaInvariants.AuditMetaInvariant(),
                new Core.Control.MetaInvariants.ReviewMetaInvariant()
            },
            new IEscalationProtocol[] { new DutyToWarnProtocol() });

        var report = evaluator.Evaluate(
            NewContext(OperationalContext.Emergency, Action("disclose-minimum")),
            InvariantScope.Plan,
            new[] { Action("disclose-full") });

        Assert.True(report.Passed);
        Assert.False(report.RequiresEscalation);
        Assert.Equal("DutyToWarn", report.Resolution.EscalationProtocol);
        var overrideDecision = Assert.Single(report.Overrides);
        Assert.Equal("PatientConfidentiality", overrideDecision.OverriddenViolation.Invariant);
        Assert.Equal("DutyToWarn", overrideDecision.EscalationProtocol);
        Assert.True(overrideDecision.RequiresReview);
    }

    // Confidentiality vs imminent harm: both fundamental. Withholding (declining) violates harm prevention.
    private static IInvariant[] ClinicalInvariants() => new IInvariant[]
    {
        new ContextualInvariant("HarmPrevention", InvariantLayer.Fundamental, InvariantCriticality.Critical,
            OperationalContext.Emergency, degree: _ => 0, declineDegree: 1.0),
        ByCapability.Of("PatientConfidentiality", InvariantLayer.Fundamental, InvariantCriticality.Critical,
            ("disclose-minimum", 0.2), ("disclose-full", 1.0))
    };

    // ---------------------------------------------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------------------------------------------

    private static AgentAction Action(string capability) => new() { Capability = capability, Input = "" };

    private static TransitionContext NewContext(
        OperationalContext operationalContext = OperationalContext.Normal,
        AgentAction? proposed = null)
    {
        return new TransitionContext
        {
            Transition = new Transition { ProposedAction = proposed ?? Action("echo") },
            OperationalContext = operationalContext
        };
    }

    /// <summary>Configurable invariant with a fixed pass/fail result, used to construct governance scenarios.</summary>
    private sealed class TestInvariant : IInvariant
    {
        private readonly bool _passed;

        private TestInvariant(
            string name,
            InvariantLayer layer,
            bool passed,
            InvariantSeverity severity = InvariantSeverity.Error,
            InvariantCriticality criticality = InvariantCriticality.Medium,
            params OperationalContext[] contexts)
        {
            Name = name;
            Layer = layer;
            _passed = passed;
            Severity = severity;
            Criticality = criticality;
            Contexts = contexts;
        }

        public string Name { get; }

        public InvariantCategory Category => InvariantCategory.Safety;

        public InvariantScope Scope => InvariantScope.Plan;

        public InvariantSeverity Severity { get; }

        public InvariantLayer Layer { get; }

        public InvariantCriticality Criticality { get; }

        public IReadOnlyList<OperationalContext> Contexts { get; }

        public InvariantResult Evaluate(TransitionContext context)
        {
            return _passed
                ? InvariantResult.Allow()
                : InvariantResult.Reject($"{Name} failed.", Severity);
        }

        public static TestInvariant Pass(string name, InvariantLayer layer) => new(name, layer, true);

        public static TestInvariant Fail(string name, InvariantLayer layer) => new(name, layer, false);

        public static TestInvariant Fail(string name, InvariantLayer layer, InvariantSeverity severity) =>
            new(name, layer, false, severity);

        public static TestInvariant Fail(
            string name,
            InvariantLayer layer,
            InvariantSeverity severity,
            InvariantCriticality criticality,
            params OperationalContext[] contexts) =>
            new(name, layer, false, severity, criticality, contexts);
    }

    /// <summary>Soft invariant whose violation degree is looked up by the candidate's capability.</summary>
    private class ByCapability : IInvariant
    {
        private readonly Dictionary<string, double> _degrees;

        protected ByCapability(string name, InvariantLayer layer, InvariantCriticality criticality,
            params (string Capability, double Degree)[] degrees)
        {
            Name = name;
            Layer = layer;
            Criticality = criticality;
            _degrees = degrees.ToDictionary(d => d.Capability, d => d.Degree);
        }

        public static ByCapability Of(string name, InvariantLayer layer, InvariantCriticality criticality,
            params (string Capability, double Degree)[] degrees) => new(name, layer, criticality, degrees);

        public string Name { get; }

        public InvariantCategory Category => InvariantCategory.Safety;

        public InvariantScope Scope => InvariantScope.Plan;

        public InvariantSeverity Severity => InvariantSeverity.Warning;

        public InvariantLayer Layer { get; }

        public InvariantCriticality Criticality { get; }

        public InvariantResult Evaluate(TransitionContext context)
        {
            var capability = context.Transition.ProposedAction?.Capability ?? "";

            return InvariantResult.Violated(
                _degrees.TryGetValue(capability, out var degree) ? degree : 0,
                $"{Name} violated by {capability}.",
                Severity);
        }
    }

    /// <summary>Soft invariant with an explicit criticality table C(I, κ).</summary>
    private sealed class TableInvariant : ByCapability, IInvariant
    {
        private readonly Dictionary<OperationalContext, InvariantCriticality> _table;

        public TableInvariant(string name, InvariantLayer layer,
            Dictionary<OperationalContext, InvariantCriticality> table,
            params (string Capability, double Degree)[] degrees)
            : base(name, layer, InvariantCriticality.Medium, degrees)
        {
            _table = table;
        }

        public InvariantCriticality CriticalityIn(OperationalContext context) =>
            _table.TryGetValue(context, out var criticality) ? criticality : InvariantCriticality.Medium;
    }

    /// <summary>Soft invariant raised by one level in a given context, with optional decline behaviour.</summary>
    private sealed class ContextualInvariant : IInvariant
    {
        private readonly Func<AgentAction?, double> _degree;
        private readonly double _declineDegree;

        public ContextualInvariant(string name, InvariantLayer layer, InvariantCriticality criticality,
            OperationalContext raisedIn, Func<AgentAction?, double>? degree = null, double declineDegree = 0)
        {
            Name = name;
            Layer = layer;
            Criticality = criticality;
            Contexts = new[] { raisedIn };
            _degree = degree ?? (_ => 0);
            _declineDegree = declineDegree;
        }

        public string Name { get; }

        public InvariantCategory Category => InvariantCategory.Safety;

        public InvariantScope Scope => InvariantScope.Plan;

        public InvariantSeverity Severity => InvariantSeverity.Warning;

        public InvariantLayer Layer { get; }

        public InvariantCriticality Criticality { get; }

        public IReadOnlyList<OperationalContext> Contexts { get; }

        public InvariantResult Evaluate(TransitionContext context) =>
            InvariantResult.Violated(_degree(context.Transition.ProposedAction), $"{Name} violated.", Severity);

        public InvariantResult EvaluateDecline(TransitionContext context) =>
            InvariantResult.Violated(_declineDegree, $"Declining violates {Name}.", Severity);
    }

    /// <summary>Pre-authorised duty-to-warn protocol: covers minimum disclosure only.</summary>
    private sealed class DutyToWarnProtocol : IEscalationProtocol
    {
        public string Name => "DutyToWarn";

        public bool Authorises(EscalationRequest request) =>
            request.Action?.Capability == "disclose-minimum" &&
            request.FundamentalViolations.All(v => v.Invariant == "PatientConfidentiality");
    }

    /// <summary>Meta-invariant that always rejects, used to verify that any meta-invariant can veto an override.</summary>
    private sealed class RejectingMetaInvariant : IMetaInvariant
    {
        public string Name => nameof(RejectingMetaInvariant);

        public MetaInvariantCategory Category => MetaInvariantCategory.Review;

        public MetaInvariantResult Evaluate(MetaInvariantContext context)
        {
            return MetaInvariantResult.Reject("Manual governance review required.");
        }
    }
}

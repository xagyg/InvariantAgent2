using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Control;
using InvariantAgent.Core.Model.Control;
using InvariantAgent.Core.Model.Transition;
using Xunit;

namespace InvariantAgent.Tests.Governance;

public sealed class HierarchicalInvariantGovernanceTests
{
    [Fact]
    public void Evaluate_WhenLowerLayerViolationHasPreservedHigherLayer_AuthorisesAuditedOverride()
    {
        var evaluator = new InvariantEvaluator(new IInvariant[]
        {
            TestInvariant.Pass("SystemIntegrity", InvariantLayer.Fundamental),
            TestInvariant.Fail(
                "PersonalisationPreference",
                InvariantLayer.AdaptiveHeuristic,
                InvariantSeverity.Warning)
        });
        var context = NewContext();

        var report = evaluator.Evaluate(context, InvariantScope.Plan);

        Assert.True(report.Passed);
        Assert.Empty(report.Violations);
        var overrideDecision = Assert.Single(report.Overrides);
        Assert.Equal("PersonalisationPreference", overrideDecision.OverriddenViolation.Invariant);
        Assert.Equal(InvariantLayer.AdaptiveHeuristic, overrideDecision.OverriddenLayer);
        Assert.Contains("SystemIntegrity", overrideDecision.PreservedHigherPriorityInvariants);
        Assert.Contains(MetaInvariantCategory.Priority, overrideDecision.MetaInvariantCategories);
        Assert.Contains(MetaInvariantCategory.OverrideSeverity, overrideDecision.MetaInvariantCategories);
        Assert.Contains(MetaInvariantCategory.Justification, overrideDecision.MetaInvariantCategories);
        Assert.Contains(MetaInvariantCategory.Audit, overrideDecision.MetaInvariantCategories);
        Assert.Contains(MetaInvariantCategory.Review, overrideDecision.MetaInvariantCategories);
        Assert.False(overrideDecision.RequiresReview);
        Assert.Empty(overrideDecision.ReviewReasons);
        Assert.Contains(
            context.Transition.Events,
            e => e.Metadata.TryGetValue("Audit", out var audit) &&
                 audit is true);
    }

    [Fact]
    public void Evaluate_WhenFundamentalInvariantFails_DoesNotOverride()
    {
        var evaluator = new InvariantEvaluator(new IInvariant[]
        {
            TestInvariant.Fail("SystemIntegrity", InvariantLayer.Fundamental),
            TestInvariant.Pass("Transparency", InvariantLayer.Behavioural)
        });

        var report = evaluator.Evaluate(NewContext(), InvariantScope.Plan);

        Assert.False(report.Passed);
        var violation = Assert.Single(report.Violations);
        Assert.Equal("SystemIntegrity", violation.Invariant);
        Assert.Empty(report.Overrides);
    }

    [Fact]
    public void Evaluate_WhenHigherLayerViolationExists_DoesNotOverrideLowerLayerViolation()
    {
        var evaluator = new InvariantEvaluator(new IInvariant[]
        {
            TestInvariant.Fail("MissionAccuracy", InvariantLayer.Mission),
            TestInvariant.Fail(
                "PersonalisationPreference",
                InvariantLayer.AdaptiveHeuristic,
                InvariantSeverity.Warning),
            TestInvariant.Pass("SystemIntegrity", InvariantLayer.Fundamental)
        });

        var report = evaluator.Evaluate(NewContext(), InvariantScope.Plan);

        Assert.False(report.Passed);
        Assert.Contains(report.Violations, v => v.Invariant == "MissionAccuracy");
        Assert.Contains(report.Violations, v => v.Invariant == "PersonalisationPreference");
        Assert.Empty(report.Overrides);
    }

    [Fact]
    public void Evaluate_WhenNoHigherLayerInvariantIsPreserved_DoesNotOverride()
    {
        var evaluator = new InvariantEvaluator(new IInvariant[]
        {
            TestInvariant.Fail("HelpfulTone", InvariantLayer.Behavioural)
        });

        var report = evaluator.Evaluate(NewContext(), InvariantScope.Plan);

        Assert.False(report.Passed);
        Assert.Single(report.Violations);
        Assert.Empty(report.Overrides);
    }

    [Fact]
    public void Evaluate_WhenSameLayerSoftViolationsAreOverridden_FlagsReview()
    {
        var evaluator = new InvariantEvaluator(new IInvariant[]
        {
            TestInvariant.Pass("SystemIntegrity", InvariantLayer.Fundamental),
            TestInvariant.Fail(
                "HelpfulTone",
                InvariantLayer.Behavioural,
                InvariantSeverity.Warning),
            TestInvariant.Fail(
                "ConciseStyle",
                InvariantLayer.Behavioural,
                InvariantSeverity.Warning)
        });
        var context = NewContext();

        var report = evaluator.Evaluate(context, InvariantScope.Plan);

        Assert.True(report.Passed);
        Assert.Equal(2, report.Overrides.Count);
        Assert.All(report.Overrides, o =>
        {
            Assert.True(o.RequiresReview);
            Assert.NotEmpty(o.ReviewReasons);
        });
        Assert.Contains(
            context.Transition.Events,
            e => e.Metadata.TryGetValue("RequiresReview", out var requiresReview) &&
                 requiresReview is true);
    }

    [Fact]
    public void Evaluate_WhenCustomMetaInvariantRejects_DoesNotOverride()
    {
        var evaluator = new InvariantEvaluator(
            new IInvariant[]
            {
                TestInvariant.Pass("SystemIntegrity", InvariantLayer.Fundamental),
                TestInvariant.Fail(
                    "PersonalisationPreference",
                    InvariantLayer.AdaptiveHeuristic,
                    InvariantSeverity.Warning)
            },
            new IMetaInvariant[]
            {
                new RejectingMetaInvariant()
            });

        var report = evaluator.Evaluate(NewContext(), InvariantScope.Plan);

        Assert.False(report.Passed);
        Assert.Single(report.Violations);
        Assert.Empty(report.Overrides);
    }

    private static TransitionContext NewContext()
    {
        return new TransitionContext
        {
            Transition = new Transition()
        };
    }

    private sealed class TestInvariant : IInvariant
    {
        private readonly bool _passed;

        private readonly InvariantSeverity _severity;

        private TestInvariant(
            string name,
            InvariantLayer layer,
            bool passed,
            InvariantSeverity severity = InvariantSeverity.Error)
        {
            Name = name;
            Layer = layer;
            _passed = passed;
            _severity = severity;
        }

        public string Name { get; }

        public InvariantCategory Category => InvariantCategory.Safety;

        public InvariantScope Scope => InvariantScope.Plan;

        public InvariantSeverity Severity => _severity;

        public InvariantLayer Layer { get; }

        public InvariantResult Evaluate(TransitionContext context)
        {
            return _passed
                ? InvariantResult.Allow()
                : InvariantResult.Reject($"{Name} failed.", Severity);
        }

        public static TestInvariant Pass(string name, InvariantLayer layer)
        {
            return new TestInvariant(name, layer, true);
        }

        public static TestInvariant Fail(string name, InvariantLayer layer)
        {
            return new TestInvariant(name, layer, false);
        }

        public static TestInvariant Fail(
            string name,
            InvariantLayer layer,
            InvariantSeverity severity)
        {
            return new TestInvariant(name, layer, false, severity);
        }
    }

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

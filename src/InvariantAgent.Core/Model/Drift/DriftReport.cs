
using InvariantAgent.Core.Model.Control;
using InvariantAgent.Core.Model.Stability;
using System.Collections.Generic;
using System.Linq;

namespace InvariantAgent.Core.Model.Drift;

/// <summary>
/// Aggregate observability report over transition history: drift, invariant failures,
/// capability usage and the behavioural stability assessment.
/// </summary>
public sealed class DriftReport
{
    /// <summary>Total number of transitions analysed.</summary>
    public int TransitionCount { get; init; }

    /// <summary>Number of rejected transitions.</summary>
    public int RejectedTransitions { get; init; }

    /// <summary>Sum of all drift record scores.</summary>
    public int TotalDriftScore { get; init; }

    /// <summary>Highest severity among drift records.</summary>
    public InvariantSeverity HighestDriftSeverity { get; init; }
        = InvariantSeverity.Info;

    /// <summary>Number of times each capability was proposed.</summary>
    public IReadOnlyDictionary<string, int> CapabilityUsage { get; init; }
        = new Dictionary<string, int>();

    /// <summary>Failure counts keyed by "InvariantName [Category]".</summary>
    public IReadOnlyDictionary<string, int> InvariantFailures { get; init; }
        = new Dictionary<string, int>();

    /// <summary>Failure counts grouped by invariant category.</summary>
    public IReadOnlyDictionary<InvariantCategory, int> InvariantFailuresByCategory { get; init; }
        = new Dictionary<InvariantCategory, int>();

    /// <summary>Number of drift records per drift type.</summary>
    public IReadOnlyDictionary<DriftType, int> DriftCounts { get; init; }
        = new Dictionary<DriftType, int>();

    /// <summary>The most recent drift records (up to 10).</summary>
    public IReadOnlyList<DriftRecord> RecentDrift { get; init; }
        = new List<DriftRecord>();

    /// <summary>Behavioural stability assessment for the same history.</summary>
    public StabilityAssessment Stability { get; init; } = new();

    /// <summary>Transitions that carry at least one HIG-C conflict record.</summary>
    public int ConflictTransitions { get; init; }

    /// <summary>Transitions held for human review because of a fundamental conflict.</summary>
    public int EscalatedTransitions { get; init; }

    /// <summary>
    /// Behavioural drift attributed to governance (IBAAM 3 revised, Section 9): drift on transitions whose conflict record
    /// prescribed a change of behaviour. Expected; replaying the log reproduces it.
    /// </summary>
    public DriftAttribution AttributedDrift { get; init; } = new();

    /// <summary>
    /// Behavioural drift the governance process does not explain: drift on transitions with no conflict record that
    /// prescribed a change. A governance signal even when every invariant is satisfied.
    /// </summary>
    public DriftAttribution UnattributedDrift { get; init; } = new();

    /// <summary>
    /// Missing expected change: conflict records whose prescribed outcome did not take effect (a declined transition that
    /// still completed, an alternative that was not the one executed, or an overridden invariant that still blocked).
    /// Suggests a resolution is not taking effect; feeds review.
    /// </summary>
    public IReadOnlyList<string> MissingExpectedChange { get; init; } = new List<string>();

    /// <summary><c>true</c> if any drift was recorded.</summary>
    public bool HasDrift => DriftCounts.Count > 0;

    /// <summary>Total number of drift records.</summary>
    public int TotalDriftEvents => DriftCounts.Values.Sum();
}

/// <summary>Count and total score of a subset of behavioural drift records.</summary>
public sealed class DriftAttribution
{
    /// <summary>Number of drift records.</summary>
    public int Events { get; init; }

    /// <summary>Sum of their scores.</summary>
    public int Score { get; init; }

    /// <summary>Share of all behavioural drift score (0 when there is none).</summary>
    public double Share { get; init; }
}

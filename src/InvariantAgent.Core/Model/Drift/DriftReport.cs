
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

    /// <summary><c>true</c> if any drift was recorded.</summary>
    public bool HasDrift => DriftCounts.Count > 0;

    /// <summary>Total number of drift records.</summary>
    public int TotalDriftEvents => DriftCounts.Values.Sum();
}

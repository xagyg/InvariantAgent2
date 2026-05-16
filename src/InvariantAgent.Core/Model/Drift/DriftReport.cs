
using InvariantAgent.Core.Model.Control;
using System.Collections.Generic;
using System.Linq;

namespace InvariantAgent.Core.Model.Drift;

public sealed class DriftReport
{
    public int TransitionCount { get; init; }

    public int RejectedTransitions { get; init; }

    public int TotalDriftScore { get; init; }

    public InvariantSeverity HighestDriftSeverity { get; init; }
        = InvariantSeverity.Info;

    public IReadOnlyDictionary<string, int> CapabilityUsage { get; init; }
        = new Dictionary<string, int>();

    public IReadOnlyDictionary<string, int> InvariantFailures { get; init; }
        = new Dictionary<string, int>();

    public IReadOnlyDictionary<DriftType, int> DriftCounts { get; init; }
        = new Dictionary<DriftType, int>();

    public IReadOnlyList<DriftRecord> RecentDrift { get; init; }
        = new List<DriftRecord>();

    public bool HasDrift => DriftCounts.Count > 0;

    public int TotalDriftEvents => DriftCounts.Values.Sum();
}
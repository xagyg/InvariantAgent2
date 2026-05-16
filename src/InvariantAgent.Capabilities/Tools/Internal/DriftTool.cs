using System.Text;
using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Capability;
using InvariantAgent.Core.Model.Data;

namespace InvariantAgent.Capabilities.Tools.Internal
{
    public sealed class DriftTool : ICapability
    {
        private readonly ITransitionStore _store;
        private readonly IDriftAnalyzer _analyzer;

        public string Name => "drift";

        public DriftTool(ITransitionStore store, IDriftAnalyzer analyzer)
        {
            _store = store;
            _analyzer = analyzer;
        }

        public CapabilityResult Execute(CapabilityRequest request)
        {
            var transitions = _store.GetAll();

            var report = _analyzer.Analyze(transitions);

            var sb = new StringBuilder();

            sb.AppendLine("\n==== DRIFT REPORT ====");
            sb.AppendLine($"Transitions: {report.TransitionCount}");
            sb.AppendLine($"Rejected: {report.RejectedTransitions}");
            sb.AppendLine($"Total drift score: {report.TotalDriftScore}");
            sb.AppendLine($"Highest drift severity: {report.HighestDriftSeverity}");

            sb.AppendLine();
            sb.AppendLine("Capability usage:");

            if (report.CapabilityUsage.Count == 0)
            {
                sb.AppendLine("  none");
            }
            else
            {
                foreach (var item in report.CapabilityUsage)
                {
                    sb.AppendLine($"  {item.Key}: {item.Value}");
                }
            }

            sb.AppendLine();
            sb.AppendLine("Invariant failures:");

            if (report.InvariantFailures.Count == 0)
            {
                sb.AppendLine("  none");
            }
            else
            {
                foreach (var item in report.InvariantFailures)
                {
                    sb.AppendLine($"  {item.Key}: {item.Value}");
                }
            }

            sb.AppendLine();
            sb.AppendLine("Drift events:");

            if (report.DriftCounts.Count == 0)
            {
                sb.AppendLine("  none");
            }
            else
            {
                foreach (var item in report.DriftCounts)
                {
                    sb.AppendLine($"  {item.Key}: {item.Value}");
                }
            }

            sb.AppendLine();
            sb.AppendLine("Recent drift:");

            if (report.RecentDrift.Count == 0)
            {
                sb.AppendLine("  none");
            }
            else
            {
                foreach (var drift in report.RecentDrift)
                {
                    sb.AppendLine(
                        $"  [{drift.Severity}] {drift.Type} Score={drift.Score} Phase={drift.Phase} Transition={drift.TransitionId}");
                    sb.AppendLine($"    {drift.Reason}");
                }
            }

            sb.Append("==== END DRIFT REPORT ====");

            return CapabilityResult.Ok(
                Name,
                new TextData
                {
                    Value = sb.ToString()
                });
        }
    }
}
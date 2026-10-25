using System.Text;
using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Capability;
using InvariantAgent.Core.Model.Data;

namespace InvariantAgent.Capabilities.Tools.Internal
{
    /// <summary><c>baseline-show</c>: displays the currently approved drift baseline.</summary>
    public sealed class BaselineShowTool : ICapability
    {
        private readonly IDriftBaselineStore _baselines;

        /// <inheritdoc />
        public string Name => "baseline-show";

        /// <summary>Creates the tool.</summary>
        /// <param name="baselines">Baseline store to read.</param>
        public BaselineShowTool(IDriftBaselineStore baselines)
        {
            _baselines = baselines;
        }

        /// <inheritdoc />
        public CapabilityResult Execute(CapabilityRequest request)
        {
            var baseline = _baselines.Current;

            if (baseline == null)
            {
                return CapabilityResult.Ok(
                    Name,
                    new TextData
                    {
                        Value = "No drift baseline has been approved."
                    });
            }

            var sb = new StringBuilder();

            sb.AppendLine("\n==== DRIFT BASELINE ====");
            sb.AppendLine($"Id: {baseline.Id}");
            sb.AppendLine($"ApprovedUtc: {baseline.ApprovedAtUtc:O}");
            sb.AppendLine($"StateVersion: {baseline.StateVersion}");
            sb.AppendLine($"Reason: {baseline.Reason}");

            if (baseline.State.Memory.Count > 0)
            {
                sb.AppendLine("Memory:");

                foreach (var item in baseline.State.Memory.OrderBy(item => item.Key))
                {
                    sb.AppendLine($"  {item.Key}={item.Value}");
                }
            }

            sb.Append("==== END DRIFT BASELINE ====");

            return CapabilityResult.Ok(
                Name,
                new TextData
                {
                    Value = sb.ToString()
                });
        }
    }
}

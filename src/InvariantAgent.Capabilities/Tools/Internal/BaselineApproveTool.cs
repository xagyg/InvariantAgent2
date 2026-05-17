using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Capability;
using InvariantAgent.Core.Model.Data;

namespace InvariantAgent.Capabilities.Tools.Internal
{
    public sealed class BaselineApproveTool : ICapability
    {
        private readonly ITransitionStore _transitions;
        private readonly IDriftBaselineStore _baselines;

        public string Name => "baseline-approve";

        public BaselineApproveTool(
            ITransitionStore transitions,
            IDriftBaselineStore baselines)
        {
            _transitions = transitions;
            _baselines = baselines;
        }

        public CapabilityResult Execute(CapabilityRequest request)
        {
            var latestState = _transitions.GetAll()
                .LastOrDefault(t => t.After != null)
                ?.After;

            if (latestState == null)
            {
                return CapabilityResult.Fail(
                    Name,
                    "No committed state is available to approve as a drift baseline.");
            }

            var reason = string.IsNullOrWhiteSpace(request.Input)
                ? "Approved via baseline-approve."
                : request.Input.Trim();

            var baseline = _baselines.Approve(latestState, reason);

            return CapabilityResult.Ok(
                Name,
                new TextData
                {
                    Value =
                        $"Approved drift baseline {baseline.Id} at state version {baseline.StateVersion}."
                });
        }
    }
}

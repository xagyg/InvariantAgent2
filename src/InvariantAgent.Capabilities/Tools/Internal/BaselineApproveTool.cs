using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Capability;
using InvariantAgent.Core.Model.Data;

namespace InvariantAgent.Capabilities.Tools.Internal
{
    /// <summary>
    /// <c>baseline-approve [reason]</c>: approves the latest committed state as the drift baseline.
    /// </summary>
    /// <remarks>
    /// After approval, behavioural drift (and the drift-bound invariant) is measured against this state.
    /// The "latest state" is the last transition with a non-null <c>After</c>, which can include a transition
    /// rejected at the Reduction checkpoint.
    /// Approval is the human-in-the-loop mechanism for accepting intentional behavioural evolution.
    /// </remarks>
    public sealed class BaselineApproveTool : ICapability
    {
        private readonly ITransitionStore _transitions;
        private readonly IDriftBaselineStore _baselines;

        /// <inheritdoc />
        public string Name => "baseline-approve";

        /// <summary>Creates the tool.</summary>
        /// <param name="transitions">Transition store used to find the latest committed state.</param>
        /// <param name="baselines">Baseline store to update.</param>
        public BaselineApproveTool(
            ITransitionStore transitions,
            IDriftBaselineStore baselines)
        {
            _transitions = transitions;
            _baselines = baselines;
        }

        /// <inheritdoc />
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

using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Control;
using InvariantAgent.Core.Model.Transition;

namespace InvariantAgent.Safety.Invariants.Outcome
{
    /// <summary>
    /// Execution-scope integrity invariant: the capability must report success. Failed executions are
    /// rejected, so they never reach state assimilation.
    /// </summary>
    public sealed class SuccessOutcomeInvariant : IInvariant
    {
        public string Name => nameof(SuccessOutcomeInvariant);

        public InvariantCategory Category => InvariantCategory.Integrity;

        public InvariantScope Scope => InvariantScope.Execution;

        public InvariantSeverity Severity => InvariantSeverity.Error;

        /// <inheritdoc />
        public InvariantResult Evaluate(TransitionContext context)
        {
            var outcome = context.Transition.Outcome;

            if (outcome == null)
            {
                return InvariantResult.Reject("No execution outcome.");
            }

            if (!outcome.Success)
            {
                return InvariantResult.Reject(
                    string.IsNullOrWhiteSpace(outcome.Error)
                        ? "Execution failed."
                        : outcome.Error);
            }

            return InvariantResult.Allow();
        }
    }
}
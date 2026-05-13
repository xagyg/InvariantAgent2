using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Control;
using InvariantAgent.Core.Model.Transition;
using System.Collections.Generic;

namespace InvariantAgent.Core.Control.Post
{
    public sealed class PostControl : IPostControl
    {
        private readonly IEnumerable<IInvariant> _invariants;

        public PostControl(IEnumerable<IInvariant> invariants)
        {
            _invariants = invariants;
        }

        public PostControlResult Evaluate(TransitionContext context)
        {
            foreach (var invariant in _invariants)
            {
                var result = invariant.Evaluate(context);

                context.Transition.Record("PostInvariant",
                    $"{invariant.Name}: {(result.Passed ? "Passed" : "Failed")} {result.Reason}");

                if (!result.Passed)
                {
                    context.Transition.Status = TransitionStatus.Rejected;

                    context.Transition.Reason =
                        $"Invariant '{invariant.Name}' failed: {result.Reason}";

                    return new PostControlResult
                    {
                        Accepted = false,
                        OriginalOutcome = context.Transition.Outcome,
                        Reason = context.Transition.Reason,
                        ViolationType = ToViolationType(invariant.Category)
                    };
                }
            }

            return new PostControlResult
            {
                Accepted = true,
                OriginalOutcome = context.Transition.Outcome
            };
        }

        private static ViolationType ToViolationType(InvariantCategory category)
        {
            return category switch
            {
                InvariantCategory.Safety => ViolationType.Safety,

                InvariantCategory.Integrity => ViolationType.Integrity,

                InvariantCategory.Identity => ViolationType.Identity,

                InvariantCategory.SelfModification => ViolationType.Safety,

                _ => ViolationType.Safety
            };
        }
    }
}
using InvariantAgent.Core.Model;
using InvariantAgent.Core.Abstractions;

namespace InvariantAgent.Core.Control.Post;

public class PostControl : IPostControl
{
    private readonly InvariantSet<AgentOutcome> _invariants;

    public PostControl(InvariantSet<AgentOutcome> invariants)
    {
        _invariants = invariants;
    }

    public PostControlResult Evaluate(AgentState state, AgentOutcome outcome)
    {
        var result = _invariants.Evaluate(outcome);

        if (result.IsValid)
        {
            return new PostControlResult
            {
                Accepted = true,
                OriginalOutcome = outcome
            };
        }

        return new PostControlResult
        {
            Accepted = false,
            OriginalOutcome = outcome,
            Reason = result.Reason,
            ViolationType = Classify(state, result)
        };
    }

    private ViolationType Classify(AgentState state, InvariantResult result)
    {
        if (result.Reason?.Contains("access") == true)
            return ViolationType.Safety;

        if (result.Reason?.Contains("state") == true)
            return ViolationType.Integrity;

        if (result.Reason?.Contains("identity") == true)
            return ViolationType.Identity;

        return ViolationType.Safety;
    }
}
using InvariantAgent.Core.Model.Agent;
using InvariantAgent.Core.Model.Drift;

namespace InvariantAgent.Core.Abstractions
{
    public interface IDriftBaselineStore
    {
        DriftBaseline Current { get; }

        DriftBaseline Approve(AgentState state, string reason);
    }
}

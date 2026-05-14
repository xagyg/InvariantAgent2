using InvariantAgent.Core.Model.Agent;
using InvariantAgent.Core.Model.Planning;
using InvariantAgent.Core.Model.Transition;
using System.Threading;
using System.Threading.Tasks;

namespace InvariantAgent.Core.Abstractions
{
    public interface IPlanner
    {
        public string Name { get; }

        Task<AgentAction> PlanAsync(PlannerContext context, CancellationToken ct = default);
    }
}
using InvariantAgent.Core.Model.Transition;
using System.Threading;
using System.Threading.Tasks;

namespace InvariantAgent.Core.Abstractions
{
    public interface IPlanner
    {
        Task PlanAsync(TransitionContext context, CancellationToken ct = default);
    }
}
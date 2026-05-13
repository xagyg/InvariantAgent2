using InvariantAgent.Core.Model.Transition;
using System.Threading;
using System.Threading.Tasks;

namespace InvariantAgent.Core.Abstractions
{
    public interface IPlanner
    {
        public string Name { get; }

        Task PlanAsync(TransitionContext context, CancellationToken ct = default);
    }
}
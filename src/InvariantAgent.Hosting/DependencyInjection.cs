using InvariantAgent.Adaptive;
using InvariantAgent.Capabilities;
using InvariantAgent.Capabilities.Services;
using InvariantAgent.Capabilities.Tools;
using InvariantAgent.Capabilities.Tools.Internal;
using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Control.Post;
using InvariantAgent.Core.Control.Pre;
using InvariantAgent.Core.Pipeline;
using InvariantAgent.Execution.Engine;
using InvariantAgent.Observability;
using InvariantAgent.Runtime;
using InvariantAgent.Safety.Invariants.Action;
using InvariantAgent.Safety.Invariants.Outcome;
using InvariantAgent.Safety.Invariants.SelfModification;
using InvariantAgent.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace InvariantAgent.Hosting
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInvariantAgent(this IServiceCollection services)
        {
            services.AddSingleton<ITransitionStore, InMemoryTransitionStore>();

            services.AddSingleton<IDriftAnalyzer, SimpleDriftAnalyzer>();

            services.AddSingleton<IPlanner, CommandPlanner>();

            services.AddSingleton<IExecutor, CapabilityExecutor>();
            services.AddSingleton<IStateReducer, StateReducer>();

            services.AddSingleton<IPreControl>(sp =>
            {
                var registry = sp.GetRequiredService<ICapabilityRegistry>();

                return new PreControl(new IInvariant[]
                {
                    new NoDeleteInvariant(),
                    new AllowedCapabilityInvariant(registry.GetCapabilityNames())
                });
            });

            services.AddSingleton<IPostControl>(_ =>
                new PostControl(new IInvariant[]
                {
                    new SuccessOutcomeInvariant(),
                    new AllowedMemoryKeyInvariant(),
                    new NonEmptyOutcomeInvariant()
                }));

            services.AddSingleton<ICapability, EchoTool>();
            services.AddSingleton<ICapability, CalculatorTool>();
            services.AddSingleton<ICapability, SearchTool>();
            services.AddSingleton<ICapability, ReplayTool>();
            services.AddSingleton<ICapability, DriftTool>();
            services.AddSingleton<ICapability, ExampleHttpService>();
            services.AddSingleton<ICapability, MemorySetTool>();
            services.AddSingleton<ICapability, MemoryShowTool>();
            services.AddSingleton<ICapability, ExplainTool>();

            services.AddSingleton<ICapabilityRegistry, CapabilityRegistry>();

            services.AddSingleton<GovernedAgentRuntime>();

            return services;
        }
    }
}
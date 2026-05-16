using InvariantAgent.Adaptive;
using InvariantAgent.Capabilities;
using InvariantAgent.Capabilities.Services;
using InvariantAgent.Capabilities.Tools;
using InvariantAgent.Capabilities.Tools.Internal;
using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Control;
using InvariantAgent.Core.Drift;
using InvariantAgent.Core.Pipeline;
using InvariantAgent.Core.Replay;
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
            // MEMORY STORAGE
            services.AddSingleton<ITransitionStore, InMemoryTransitionStore>();

            // PERSISTENT STORAGE (Comment out the memory storage above, and uncomment the lines below) ...
            //services.AddSingleton<ITransitionStore>(_ => 
            //        new FileTransitionStore(Path.Combine(AppContext.BaseDirectory, "data", "transitions.json")));

            services.AddSingleton<IDriftAnalyzer, SimpleDriftAnalyzer>();

            services.AddSingleton<IPlanner, CommandPlanner>();

            services.AddSingleton<ReplayValidator>();

            services.AddSingleton<DriftTracker>();

            services.AddSingleton<IExecutor, CapabilityExecutor>();
            services.AddSingleton<IStateReducer, StateReducer>();

            services.AddSingleton<IInvariantEvaluator>(sp =>
            {
                var registry = sp.GetRequiredService<ICapabilityRegistry>();

                return new InvariantEvaluator(new IInvariant[]
                {
                    new NoDeleteInvariant(),
                    new AllowedCapabilityInvariant(registry.GetCapabilityNames()),
                    new SuccessOutcomeInvariant(),
                    new AllowedMemoryKeyInvariant(),
                    new NonEmptyOutcomeInvariant()
                });
            });

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
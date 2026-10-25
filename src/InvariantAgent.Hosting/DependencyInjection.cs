using InvariantAgent.Adaptive;
using InvariantAgent.Capabilities;
using InvariantAgent.Capabilities.Services;
using InvariantAgent.Capabilities.Tools;
using InvariantAgent.Capabilities.Tools.Internal;
using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Control;
using InvariantAgent.Core.Control.MetaInvariants;
using InvariantAgent.Core.Drift;
using InvariantAgent.Core.Pipeline;
using InvariantAgent.Core.Replay;
using InvariantAgent.Execution.Engine;
using InvariantAgent.Observability;
using InvariantAgent.Runtime;
using InvariantAgent.Safety.Invariants.Action;
using InvariantAgent.Safety.Invariants.Identity;
using InvariantAgent.Safety.Invariants.Outcome;
using InvariantAgent.Safety.Invariants.SelfModification;
using InvariantAgent.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace InvariantAgent.Hosting
{
    /// <summary>
    /// Composition root: registers all InvariantAgent components with dependency injection.
    /// </summary>
    public static class DependencyInjection
    {
        /// <summary>
        /// Registers storage, observability, planner, executor, reducer, meta-invariants, invariants,
        /// capabilities and the governed runtime (all as singletons).
        /// </summary>
        /// <param name="services">The service collection to populate.</param>
        /// <returns>The same service collection, for chaining.</returns>
        public static IServiceCollection AddInvariantAgent(this IServiceCollection services)
        {
            // MEMORY STORAGE
            services.AddSingleton<ITransitionStore, InMemoryTransitionStore>();

            // PERSISTENT STORAGE (Comment out the memory storage above, and uncomment the lines below) ...
            //services.AddSingleton<ITransitionStore>(_ => 
            //        new FileTransitionStore(Path.Combine(AppContext.BaseDirectory, "data", "transitions.json")));

            // OBSERVABILITY: drift analysis and behavioural stability evaluation.
            services.AddSingleton<IDriftAnalyzer, SimpleDriftAnalyzer>();
            services.AddSingleton<IStabilityEvaluator, TransitionHistoryStabilityEvaluator>();

            // ADAPTIVE: swap in OpenAiPlanner/GeminiPlanner here to use an LLM planner.
            services.AddSingleton<IPlanner, CommandPlanner>();

            services.AddSingleton<ReplayValidator>();

            services.AddSingleton<DriftTracker>();
            services.AddSingleton<IDriftBaselineStore, InMemoryDriftBaselineStore>();
            services.AddSingleton<BehaviouralDriftDetector>();

            // EXECUTION and STATE ASSIMILATION.
            services.AddSingleton<IExecutor, CapabilityExecutor>();
            services.AddSingleton<IStateReducer, StateReducer>();

            // HIG META-INVARIANTS: govern whether lower-priority violations may be overridden.
            services.AddSingleton<IMetaInvariant, PriorityMetaInvariant>();
            services.AddSingleton<IMetaInvariant, OverrideSeverityMetaInvariant>();
            services.AddSingleton<IMetaInvariant, JustificationMetaInvariant>();
            services.AddSingleton<IMetaInvariant, AuditMetaInvariant>();
            services.AddSingleton<IMetaInvariant, ReviewMetaInvariant>();

            // CONTROL: the active invariant set. Each invariant declares its own scope (lifecycle checkpoint).
            services.AddSingleton<IInvariantEvaluator>(sp =>
            {
                var registry = sp.GetRequiredService<ICapabilityRegistry>();
                var metaInvariants = sp.GetServices<IMetaInvariant>();

                return new InvariantEvaluator(
                    new IInvariant[]
                    {
                        new NoDeleteInvariant(),
                        new AllowedCapabilityInvariant(registry.GetCapabilityNames()),
                        new SuccessOutcomeInvariant(),
                        new AllowedMemoryKeyInvariant(),
                        new NonEmptyOutcomeInvariant(),
                        new BehaviouralDriftBoundInvariant(
                            sp.GetRequiredService<BehaviouralDriftDetector>())
                    },
                    metaInvariants);
            });

            // CAPABILITIES: user-facing tools followed by internal introspection tools.
            services.AddSingleton<ICapability, EchoTool>();
            services.AddSingleton<ICapability, CalculatorTool>();
            services.AddSingleton<ICapability, SearchTool>();
            services.AddSingleton<ICapability, ReplayTool>();
            services.AddSingleton<ICapability, DriftTool>();
            services.AddSingleton<ICapability, ExampleHttpService>();
            services.AddSingleton<ICapability, MemorySetTool>();
            services.AddSingleton<ICapability, MemoryShowTool>();
            services.AddSingleton<ICapability, ExplainTool>();
            services.AddSingleton<ICapability, BaselineApproveTool>();
            services.AddSingleton<ICapability, BaselineShowTool>();

            services.AddSingleton<ICapabilityRegistry, CapabilityRegistry>();

            // RUNTIME: orchestrates the governed transition lifecycle.
            services.AddSingleton<GovernedAgentRuntime>();

            return services;
        }
    }
}

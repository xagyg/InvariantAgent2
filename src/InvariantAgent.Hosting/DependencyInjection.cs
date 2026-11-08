using InvariantAgent.Adaptive;
using InvariantAgent.Core.Model.Control;
using InvariantAgent.Safety.Invariants.Control;
using InvariantAgent.Capabilities;
using InvariantAgent.Capabilities.Services;
using InvariantAgent.Capabilities.Tools;
using InvariantAgent.Capabilities.Tools.Internal;
using InvariantAgent.Conscience;
using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Control;
using InvariantAgent.Core.Conscience;
using InvariantAgent.Core.Control.MetaInvariants;
using InvariantAgent.Core.Drift;
using InvariantAgent.Core.Pipeline;
using InvariantAgent.Core.Replay;
using InvariantAgent.Execution.Engine;
using InvariantAgent.Observability;
using InvariantAgent.Runtime;
using InvariantAgent.Safety.Invariants.Action;
using InvariantAgent.Safety.Invariants.Identity;
using InvariantAgent.Safety.Invariants.Mission;
using InvariantAgent.Safety.Invariants.Outcome;
using InvariantAgent.Safety.Invariants.Runtime;
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
        /// <summary>File name of the transition history inside the data directory.</summary>
        public const string TransitionsFileName = "transitions.jsonl";

        /// <summary>File name of the drift baseline approval history inside the data directory.</summary>
        public const string BaselinesFileName = "baselines.jsonl";

        /// <summary>File name of the conscience commitment history inside the data directory.</summary>
        public const string CommitmentsFileName = "commitments.jsonl";

        /// <summary>
        /// Registers storage, observability, planner, executor, reducer, meta-invariants, invariants,
        /// capabilities and the governed runtime (all as singletons).
        /// </summary>
        /// <param name="services">The service collection to populate.</param>
        /// <param name="dataDirectory">
        /// Directory for persistent storage. When set, transitions, drift baselines and conscience commitments are kept
        /// in <see cref="TransitionsFileName"/>, <see cref="BaselinesFileName"/> and <see cref="CommitmentsFileName"/>
        /// there and survive restarts (the runtime resumes from the last committed state). When <c>null</c>, everything
        /// is in memory.
        /// </param>
        /// <param name="configureConscience">
        /// Configures the functional conscience (IBAAM 6). It is enabled by default; set
        /// <see cref="ConscienceOptions.Enabled"/> to false to run with invariants only.
        /// </param>
        /// <returns>The same service collection, for chaining.</returns>
        public static IServiceCollection AddInvariantAgent(
            this IServiceCollection services,
            string? dataDirectory = null,
            Action<ConscienceOptions>? configureConscience = null,
            OperationalContext operationalContext = OperationalContext.Normal)
        {
            var conscienceOptions = new ConscienceOptions();
            configureConscience?.Invoke(conscienceOptions);

            // STORAGE: transition history and approved drift baselines.
            if (string.IsNullOrWhiteSpace(dataDirectory))
            {
                services.AddSingleton<ITransitionStore, InMemoryTransitionStore>();
                services.AddSingleton<IDriftBaselineStore, InMemoryDriftBaselineStore>();
                services.AddSingleton<ICommitmentStore, InMemoryCommitmentStore>();
            }
            else
            {
                services.AddSingleton<ITransitionStore>(_ =>
                    new FileTransitionStore(Path.Combine(dataDirectory, TransitionsFileName)));
                services.AddSingleton<IDriftBaselineStore>(_ =>
                    new FileDriftBaselineStore(Path.Combine(dataDirectory, BaselinesFileName)));
                services.AddSingleton<ICommitmentStore>(_ =>
                    new FileCommitmentStore(Path.Combine(dataDirectory, CommitmentsFileName)));
            }

            // OBSERVABILITY: drift analysis and behavioural stability evaluation.
            services.AddSingleton<IDriftAnalyzer, SimpleDriftAnalyzer>();
            services.AddSingleton<IStabilityEvaluator, TransitionHistoryStabilityEvaluator>();

            // ADAPTIVE: swap in OpenAiPlanner/GeminiPlanner here to use an LLM planner.
            services.AddSingleton<IPlanner, CommandPlanner>();

            services.AddSingleton<ReplayValidator>();

            services.AddSingleton<DriftTracker>();
            services.AddSingleton<BehaviouralDriftDetector>();

            // EXECUTION and STATE ASSIMILATION.
            services.AddSingleton<IExecutor, CapabilityExecutor>();
            services.AddSingleton<IStateReducer, StateReducer>();

            // HIG-C OPERATIONAL CONTEXT: stamped on every transition; changed only by an operator (the context capability).
            services.AddSingleton(new OperationalContextState(
                operationalContext,
                operationalContext == OperationalContext.Normal ? "default" : "configured at start-up"));

            // HIG-C META-INVARIANTS (IBAAM 3 revised, Section 7): fundamental violations need escalation (Priority);
            // hard severities stay blocking (deployment policy); overrides must be witnessed (Justification); every
            // override is audited; ambiguous ones are flagged for review.
            services.AddSingleton<IMetaInvariant, PriorityMetaInvariant>();
            services.AddSingleton<IMetaInvariant, OverrideSeverityMetaInvariant>();
            services.AddSingleton<IMetaInvariant, JustificationMetaInvariant>();
            services.AddSingleton<IMetaInvariant, AuditMetaInvariant>();
            services.AddSingleton<IMetaInvariant, ReviewMetaInvariant>();

            // CONTROL: the active invariant set. Each invariant declares its own scope (lifecycle checkpoint).
            // Registered individually (in evaluation order) so the conscience can read the value each one protects.
            services.AddSingleton<IInvariant, NoDeleteInvariant>();
            services.AddSingleton<IInvariant>(sp =>
                new AllowedCapabilityInvariant(sp.GetRequiredService<ICapabilityRegistry>().GetCapabilityNames()));
            services.AddSingleton<IInvariant, SuccessOutcomeInvariant>();
            services.AddSingleton<IInvariant, AllowedMemoryKeyInvariant>();
            services.AddSingleton<IInvariant, NonEmptyOutcomeInvariant>();
            services.AddSingleton<IInvariant>(sp =>
                new BehaviouralDriftBoundInvariant(sp.GetRequiredService<BehaviouralDriftDetector>()));
            // Control integrity: governance capabilities (review, baseline, context) run only when the operator typed them.
            services.AddSingleton<IInvariant>(sp =>
            {
                var registry = sp.GetRequiredService<ICapabilityRegistry>();
                return new OperatorOnlyCapabilityInvariant(
                    registry.GetCapabilityNames().Where(n => registry.Get(n)?.OperatorOnly == true));
            });
            // Backstop: a rejected transition must never carry state into a commit.
            services.AddSingleton<IInvariant, RejectedTransitionCannotCommitInvariant>();
            // HIG-C: the duty to act on the request gives declining its cost (IBAAM 3 revised, Section 8.1). Without it,
            // declining satisfies everything and any violation at all rejects the transition.
            services.AddSingleton<IInvariant, ServiceObligationInvariant>();

            // PRE-AUTHORISED PROTOCOLS for fundamental-level conflicts (Section 7.2): none by default, so a conflict
            // between fundamental invariants is always held for human review. Register IEscalationProtocol
            // implementations here only with the approval of the responsible governance body.
            services.AddSingleton<IInvariantEvaluator>(sp =>
                new InvariantEvaluator(
                    sp.GetServices<IInvariant>(),
                    sp.GetServices<IMetaInvariant>(),
                    sp.GetServices<IEscalationProtocol>()));

            // CONSCIENCE (IBAAM 6): weighs what the agent should do inside what the invariants say it may do.
            // Deliberately not an IInvariant: it has its own contract and runs only after the invariants pass.
            services.AddSingleton(conscienceOptions);
            services.AddSingleton<ConscienceSession>();

            if (conscienceOptions.Enabled)
            {
                services.AddSingleton<IConscience>(sp =>
                    new HistoryCalibratedConscience(conscienceOptions, sp.GetService<IValueJudge>()));
                services.AddSingleton<ConscienceCoordinator>();
            }

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
            services.AddSingleton<ICapability, ConscienceShowTool>();
            services.AddSingleton<ICapability, ConscienceReviewTool>();
            services.AddSingleton<ICapability, ContextTool>();

            services.AddSingleton<ICapabilityRegistry, CapabilityRegistry>();

            // RUNTIME: orchestrates the governed transition lifecycle.
            services.AddSingleton<GovernedAgentRuntime>();

            return services;
        }
    }
}

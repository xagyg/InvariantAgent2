using InvariantAgent.Conscience;
using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Agent;
using InvariantAgent.Core.Model.Planning;
using InvariantAgent.Core.Parsing;
using InvariantAgent.Hosting;
using InvariantAgent.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace InvariantAgent.Tests.Conscience;

/// <summary>
/// Builds a runtime from the production composition root with hooks for conscience tests: conscience options, a shared
/// transition or commitment store, a custom planner, or a reduced invariant set (leave-one-out).
/// </summary>
internal sealed class ConscienceTestHost
{
    private ConscienceTestHost(ServiceProvider provider)
    {
        Provider = provider;
        Runtime = provider.GetRequiredService<GovernedAgentRuntime>();
        Store = provider.GetRequiredService<ITransitionStore>();
        Commitments = provider.GetRequiredService<ICommitmentStore>();
        Coordinator = provider.GetService<ConscienceCoordinator>();
    }

    public ServiceProvider Provider { get; }

    public GovernedAgentRuntime Runtime { get; }

    public ITransitionStore Store { get; }

    public ICommitmentStore Commitments { get; }

    public ConscienceCoordinator? Coordinator { get; }

    public static ConscienceTestHost Create(
        Action<ConscienceOptions>? configure = null,
        ITransitionStore? sharedStore = null,
        ICommitmentStore? sharedCommitments = null,
        IPlanner? planner = null,
        Type? removeInvariant = null,
        string? dataDirectory = null)
    {
        var services = new ServiceCollection();

        services.AddInvariantAgent(dataDirectory, configure);

        if (sharedStore != null)
        {
            services.AddSingleton(sharedStore);
        }

        if (sharedCommitments != null)
        {
            services.AddSingleton(sharedCommitments);
        }

        if (planner != null)
        {
            services.AddSingleton(planner);
        }

        if (removeInvariant != null)
        {
            var descriptor = services.Single(d =>
                d.ServiceType == typeof(IInvariant) && d.ImplementationType == removeInvariant);

            services.Remove(descriptor);
        }

        return new ConscienceTestHost(services.BuildServiceProvider(validateScopes: true));
    }
}

/// <summary>
/// Command planner that responds to conscience feedback: when asked to revise a memory write, it proposes writing the
/// same value to an allowed key instead (<c>notes</c>). Used to test deliberative influence.
/// </summary>
internal sealed class RevisingPlanner : IPlanner
{
    public string Name => "revising";

    public int RevisionRequests { get; private set; }

    public Task<AgentAction> PlanAsync(PlannerContext context, CancellationToken ct = default)
    {
        var action = AgentActionParser.Parse(context.Input);

        if (context.Revision != null)
        {
            RevisionRequests++;

            var value = action.Input.Contains('=') ? action.Input.Split('=', 2)[1] : action.Input;

            return Task.FromResult(new AgentAction { Capability = "memory-set", Input = $"notes={value}" });
        }

        return Task.FromResult(action);
    }
}

/// <summary>Planner that, when asked to revise, only makes cosmetic changes (so revision never resolves the concern).</summary>
internal sealed class StubbornPlanner : IPlanner
{
    public string Name => "stubborn";

    public Task<AgentAction> PlanAsync(PlannerContext context, CancellationToken ct = default)
    {
        var action = AgentActionParser.Parse(context.Input);

        if (context.Revision != null)
        {
            action.Input += new string('!', context.Revision.Attempt);
        }

        return Task.FromResult(action);
    }
}

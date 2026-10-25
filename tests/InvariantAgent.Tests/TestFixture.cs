
using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Drift;
using InvariantAgent.Hosting;
using InvariantAgent.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace InvariantAgent.Tests;

/// <summary>
/// Builds a fully wired runtime from the production composition root (<c>AddInvariantAgent</c>)
/// and exposes the services tests need to inspect. Each call creates an isolated container.
/// </summary>
internal sealed record TestFixture(
    GovernedAgentRuntime Runtime,
    ITransitionStore Store,
    IDriftAnalyzer DriftAnalyzer,
    DriftTracker DriftTracker,
    IDriftBaselineStore DriftBaselineStore)
{
    /// <summary>Creates a fresh, isolated fixture.</summary>
    public static TestFixture Create()
    {
        var services = new ServiceCollection();

        services.AddInvariantAgent();

        var provider = services.BuildServiceProvider(
            validateScopes: true);

        return new TestFixture(
            provider.GetRequiredService<GovernedAgentRuntime>(),
            provider.GetRequiredService<ITransitionStore>(),
            provider.GetRequiredService<IDriftAnalyzer>(),
            provider.GetRequiredService<DriftTracker>(),
            provider.GetRequiredService<IDriftBaselineStore>());
    }
}


using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Drift;
using InvariantAgent.Hosting;
using InvariantAgent.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace InvariantAgent.Tests;

internal sealed record TestFixture(
    GovernedAgentRuntime Runtime,
    ITransitionStore Store,
    IDriftAnalyzer DriftAnalyzer,
    DriftTracker DriftTracker)
{
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
            provider.GetRequiredService<DriftTracker>());
    }
}

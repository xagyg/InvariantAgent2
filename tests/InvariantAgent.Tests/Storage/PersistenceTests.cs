using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Drift;
using InvariantAgent.Core.Model.Agent;
using InvariantAgent.Core.Model.Transition;
using InvariantAgent.Hosting;
using InvariantAgent.Runtime;
using InvariantAgent.Storage;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace InvariantAgent.Tests.Storage;

/// <summary>
/// Persistent storage: history, baselines and committed state survive a restart, and records read back
/// from disk are equivalent to the live ones (so replay of reloaded history matches the original decisions).
/// </summary>
public sealed class PersistenceTests : IDisposable
{
    private readonly string _dataDirectory =
        Path.Combine(Path.GetTempPath(), "invariantagent-tests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_dataDirectory))
        {
            Directory.Delete(_dataDirectory, recursive: true);
        }
    }

    // A new service provider over the same data directory simulates restarting the application.
    private ServiceProvider Start()
    {
        var services = new ServiceCollection();
        services.AddInvariantAgent(_dataDirectory);
        return services.BuildServiceProvider(validateScopes: true);
    }

    [Fact]
    public async Task Restart_RestoresHistoryCommittedStateAndBaseline()
    {
        int committedVersion;
        int transitionCount;
        Guid baselineId;

        using (var first = Start())
        {
            var runtime = first.GetRequiredService<GovernedAgentRuntime>();

            await runtime.RunAsync("memory-set goal=alpha");
            await runtime.RunAsync("baseline-approve reviewed");
            await runtime.RunAsync("echo hello");

            committedVersion = runtime.State.Version;
            transitionCount = first.GetRequiredService<ITransitionStore>().GetAll().Count;
            baselineId = first.GetRequiredService<IDriftBaselineStore>().Current.Id;
        }

        using var second = Start();

        var restarted = second.GetRequiredService<GovernedAgentRuntime>();
        var baseline = second.GetRequiredService<IDriftBaselineStore>().Current;

        Assert.Equal(transitionCount, second.GetRequiredService<ITransitionStore>().GetAll().Count);
        Assert.Equal(committedVersion, restarted.State.Version);
        Assert.Equal("alpha", Assert.IsType<string>(restarted.State.Memory["goal"]));

        Assert.NotNull(baseline);
        Assert.Equal(baselineId, baseline.Id);
        Assert.Equal("alpha", Assert.IsType<string>(baseline.State.Memory["goal"]));

        // The next transition continues from the restored state rather than from an empty one.
        var next = await restarted.RunAsync("echo again");

        Assert.Equal(committedVersion, next.Transition.Before.Version);
        Assert.Equal(committedVersion + 1, restarted.State.Version);
    }

    [Fact]
    public async Task ReloadedTransitions_KeepPrimitiveTypesInMemoryAndMetadata()
    {
        using (var first = Start())
        {
            await first.GetRequiredService<GovernedAgentRuntime>().RunAsync("memory-set goal=typed");
        }

        using var second = Start();

        var transition = Assert.Single(second.GetRequiredService<ITransitionStore>().GetAll());
        var invariantEvent = transition.Events.First(e => e.Stage == TransitionEventStage.Invariant);

        Assert.IsType<string>(transition.After.Memory["goal"]);
        Assert.IsType<string>(invariantEvent.Metadata["Scope"]);
        Assert.IsType<bool>(invariantEvent.Metadata["Passed"]);
    }

    [Fact]
    public async Task ReplayValidate_OnReloadedHistory_ReproducesEveryDecisionWithoutRecordingDrift()
    {
        using (var first = Start())
        {
            var runtime = first.GetRequiredService<GovernedAgentRuntime>();

            await runtime.RunAsync("memory-set goal=alpha");
            await runtime.RunAsync("baseline-approve reviewed");
            await runtime.RunAsync("memory-set goal=beta");
            await runtime.RunAsync("memory-set secret=nope"); // rejected: key not allowed
            await runtime.RunAsync("echo done");
        }

        using var second = Start();

        var context = await second.GetRequiredService<GovernedAgentRuntime>().RunAsync("replay validate");
        var output = context.Transition.Outcome?.Result ?? "";

        Assert.Contains("==== REPLAY VALIDATION START ====", output);
        Assert.Contains("Rejected] Passed", output);
        Assert.DoesNotContain("Drift=", output);
        Assert.Empty(second.GetRequiredService<DriftTracker>().Records);
    }

    [Fact]
    public async Task Transitions_RecordTheDriftBaselineInForce()
    {
        using var provider = Start();

        var runtime = provider.GetRequiredService<GovernedAgentRuntime>();

        var beforeApproval = await runtime.RunAsync("memory-set goal=alpha");
        await runtime.RunAsync("baseline-approve reviewed");
        var afterApproval = await runtime.RunAsync("echo hello");

        var current = provider.GetRequiredService<IDriftBaselineStore>().Current;

        Assert.NotNull(beforeApproval.Transition.DriftBaseline);
        Assert.Null(beforeApproval.Transition.DriftBaseline.BaselineId);

        Assert.Equal(current.Id, afterApproval.Transition.DriftBaseline.BaselineId);
        Assert.Equal("alpha", afterApproval.Transition.DriftBaseline.State.Memory["goal"]);
    }

    [Fact]
    public void FileStore_SkipsTornFinalLine()
    {
        var path = Path.Combine(_dataDirectory, "torn.jsonl");
        var store = new FileTransitionStore(path);

        store.Append(new Transition { Input = "kept", Before = new AgentState() });
        File.AppendAllText(path, "{\"Id\":\"0000");

        Assert.Equal("kept", Assert.Single(store.GetAll()).Input);
    }

    [Fact]
    public void FileStore_ThrowsOnCorruptLineInsideHistory()
    {
        var path = Path.Combine(_dataDirectory, "corrupt.jsonl");
        var store = new FileTransitionStore(path);

        store.Append(new Transition { Input = "first", Before = new AgentState() });
        File.AppendAllText(path, "not json" + Environment.NewLine);
        store.Append(new Transition { Input = "third", Before = new AgentState() });

        var ex = Assert.Throws<InvalidDataException>(() => store.GetAll());
        Assert.Contains("line 2", ex.Message);
    }
}

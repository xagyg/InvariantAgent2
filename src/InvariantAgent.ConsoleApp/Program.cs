using InvariantAgent.Hosting;
using InvariantAgent.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace InvariantAgent.ConsoleApp
{
    /// <summary>
    /// Console entry point. Hosts the governed runtime as an interactive REPL using the .NET Generic Host.
    /// </summary>
    internal static class Program
    {
        /// <summary>Configures logging and services, then runs the host until the user exits.</summary>
        /// <param name="args">Command-line arguments passed to the host builder.</param>
        public static async Task Main(string[] args)
        {
            await Host.CreateDefaultBuilder(args)
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Warning);
                })
                .ConfigureServices(services =>
                {
                    // Registers the runtime, invariants, planner, capabilities, storage and observability.
                    services.AddInvariantAgent();

                    // Runs the interactive REPL loop in the background.
                    services.AddHostedService<
                        GovernedAgentRuntimeHostedService>();
                })
                .Build()
                .RunAsync();
        }
    }
}
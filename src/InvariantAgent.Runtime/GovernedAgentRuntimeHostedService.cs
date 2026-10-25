using Microsoft.Extensions.Hosting;

namespace InvariantAgent.Runtime
{
    /// <summary>
    /// Hosted background service providing the interactive console REPL for the governed runtime.
    /// </summary>
    /// <remarks>
    /// Each line of input runs one governed transition; the transition's events are printed with
    /// stage-specific colours. <c>exit</c>/<c>quit</c> stops the host; <c>clear</c> clears the screen.
    /// </remarks>
    public sealed class GovernedAgentRuntimeHostedService
        : BackgroundService
    {
        private readonly GovernedAgentRuntime _runtime;
        private readonly IHostApplicationLifetime _lifetime;

        /// <summary>Creates the hosted service.</summary>
        /// <param name="runtime">The governed runtime.</param>
        /// <param name="lifetime">Host lifetime used to stop the application on exit.</param>
        public GovernedAgentRuntimeHostedService(
            GovernedAgentRuntime runtime, IHostApplicationLifetime lifetime)
        {
            _runtime = runtime;
            _lifetime = lifetime;
        }

        /// <summary>Runs the REPL loop until cancellation or the user exits.</summary>
        /// <param name="stoppingToken">Signalled when the host is shutting down.</param>
        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            Console.WriteLine("=== InvariantAgent REPL ===");
            Console.WriteLine("Commands:");
            Console.WriteLine("  exit  - quit");
            Console.WriteLine("  clear - clear screen");
            Console.WriteLine();

            while (!stoppingToken.IsCancellationRequested)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write("agent> ");
                Console.ResetColor();

                var input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input))
                    continue;

                input = input.Trim();

                if (input is "exit" or "quit")
                {
                    //break;  ok for console but for hosted ...
                    _lifetime.StopApplication();
                    return;
                }

                if (input == "clear")
                {
                    Console.Clear();
                    continue;
                }

                var context = await _runtime.RunAsync(input);

                foreach (var e in context.Transition.Events)
                {
                    Console.ForegroundColor = GetColor(e.Stage.ToString());

                    Console.Write($"[{e.Stage}] ");

                    Console.ResetColor();

                    Console.WriteLine(e.Message);
                }

                Console.WriteLine($"Transition Id: {context.Transition.Id}");
                Console.WriteLine($"Status: {context.Transition.Status}");
                Console.WriteLine();
            }
        }

        // Console colour for each transition event stage.
        private static ConsoleColor GetColor(string stage)
        {
            return stage switch
            {
                "Input" => ConsoleColor.Cyan,
                "Planning" => ConsoleColor.Cyan,                
                "Invariant" => ConsoleColor.Yellow,
                "Control" => ConsoleColor.Yellow,              
                "Execution" => ConsoleColor.Green,
                "Reduction" => ConsoleColor.Cyan,
                "SelfModification" => ConsoleColor.Blue,
                "Lifecycle" => ConsoleColor.White,
                _ => ConsoleColor.Gray
            };
        }
    }
}
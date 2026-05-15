using Microsoft.Extensions.Hosting;

namespace InvariantAgent.Runtime
{
    public sealed class GovernedAgentRuntimeHostedService
        : BackgroundService
    {
        private readonly GovernedAgentRuntime _runtime;
        private readonly IHostApplicationLifetime _lifetime;

        public GovernedAgentRuntimeHostedService(
            GovernedAgentRuntime runtime, IHostApplicationLifetime lifetime)
        {
            _runtime = runtime;
            _lifetime = lifetime;
        }

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

                Console.WriteLine($"Status: {context.Transition.Status}");
                Console.WriteLine();
            }
        }

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
                _ => ConsoleColor.Gray
            };
        }
    }
}
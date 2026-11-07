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
            Console.WriteLine($"Resumed at state version {_runtime.State.Version}.");
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

                if (context.Transition.Status == Core.Model.Transition.TransitionStatus.Rejected &&
                    !string.IsNullOrWhiteSpace(context.Transition.Reason))
                {
                    Console.WriteLine($"Reason: {context.Transition.Reason}");
                }

                PrintReviewCommands(context.Transition);
                Console.WriteLine();
            }
        }

        // When the conscience escalated, prints the review commands ready to copy. Deliberately print-only: escalation means
        // someone decides separately, so the REPL never prompts the requester to approve their own request.
        private static void PrintReviewCommands(Core.Model.Transition.Transition transition)
        {
            var escalation = transition.Appraisals?.LastOrDefault();

            if (escalation == null || escalation.Effect != Core.Model.Conscience.ConscienceEffect.Escalated)
            {
                return;
            }

            var id = transition.Id.ToString("N")[..8];

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Held for human review. To resolve, run one of:");
            Console.ResetColor();
            Console.WriteLine($"  conscience-review {id} uphold <note>     (decline this and similar requests)");
            Console.WriteLine($"  conscience-review {id} overturn <note>   (clear it; then retry the request)");
            Console.WriteLine("  conscience pending                           (see everything awaiting review)");
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
                "Conscience" => ConsoleColor.Red,
                _ => ConsoleColor.Gray
            };
        }
    }
}
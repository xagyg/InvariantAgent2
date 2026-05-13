using InvariantAgent.Adaptive;
using InvariantAgent.Capabilities;
using InvariantAgent.Capabilities.Services;
using InvariantAgent.Capabilities.Tools;
using InvariantAgent.Capabilities.Tools.Internal;
using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Control.Post;
using InvariantAgent.Core.Control.Pre;
using InvariantAgent.Core.Pipeline;
using InvariantAgent.Execution.Engine;
using InvariantAgent.Safety.Invariants.Action;
using InvariantAgent.Safety.Invariants.Outcome;
using InvariantAgent.Runtime;
using InvariantAgent.Storage;
using InvariantAgent.Observability;

namespace InvariantAgent.Demno;

// FOR EXPERIMENTAL PURPOSES ONLY (Startup Program is in InvariantAgent.ConsoleApp)

internal static class Program
{
    static void Main()
    {
        Console.WriteLine("=== InvariantAgent REPL ===");
        Console.WriteLine("Commands:");
        Console.WriteLine("  exit      - quit");
        Console.WriteLine("  clear     - clear screen");
        Console.WriteLine();

        var orchestrator = BuildOrchestrator();

        while (true)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("agent> ");
            Console.ResetColor();

            var input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
                continue;

            input = input.Trim();

            switch (input.ToLowerInvariant())
            {
                case "exit":
                case "quit":
                    return;

                case "clear":
                    Console.Clear();
                    continue;
            }

            try
            {
                Run(orchestrator, input);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ERROR] {ex.Message}");
                Console.ResetColor();
            }

            Console.WriteLine();
        }
    }

    private static GovernedAgentRuntime BuildOrchestrator()
    {
        var transitionStore = new InMemoryTransitionStore();

        var registry = new CapabilityRegistry(new List<ICapability>
        {
            new EchoTool(),
            new SearchTool(),
            new CalculatorTool(),
            new AuditTool(transitionStore),
            new ExampleHttpService(),
            new DriftTool(transitionStore, new SimpleDriftAnalyzer())
        });

        var preInvariants = new List<IInvariant>
        {
            new NoDeleteInvariant(),
            new AllowedCapabilityInvariant(registry.GetCapabilityNames())
        };

        var postInvariants = new List<IInvariant>
        {
            new SuccessOutcomeInvariant(),
            new NonEmptyOutcomeInvariant()
        };

        var pre = new PreControl(preInvariants);
        var post = new PostControl(postInvariants);
        var executor = new CapabilityExecutor(registry);
        var planner = CreatePlanner("command");

        return new GovernedAgentRuntime(
            planner,
            pre,
            post,
            executor,
            new StateReducer(),
            transitionStore);
    }

    private static void Run(GovernedAgentRuntime orchestrator, string input)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine();
        Console.WriteLine($"INPUT: {input}");
        Console.ResetColor();

        var context = orchestrator
            .RunAsync(input)
            .GetAwaiter()
            .GetResult();

        var transition = context.Transition;

        Console.WriteLine();

        foreach (var e in transition.Events)
        {
            Console.ForegroundColor = GetColor(e.Stage.ToString());
            Console.Write($"[{e.Stage}] ");
            Console.ResetColor();
            Console.WriteLine(e.Message);
        }

        if (!string.IsNullOrWhiteSpace(transition.Reason))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Reason: {transition.Reason}");
            Console.ResetColor();
        }

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"Status: {transition.Status}");
        Console.ResetColor();
    }

    private static ConsoleColor GetColor(string stage)
    {
        return stage switch
        {
            "Input" => ConsoleColor.Cyan,
            "Planning" => ConsoleColor.Cyan,
            "Invariant" => ConsoleColor.DarkCyan,
            "PreControl" => ConsoleColor.Yellow,
            "PostControl" => ConsoleColor.Yellow,
            "Execution" => ConsoleColor.Green,
            "Reducer" => ConsoleColor.Cyan,
            "Rejected" => ConsoleColor.Red,
            _ => ConsoleColor.Gray
        };
    }

    private static IPlanner CreatePlanner(string mode, params string[] args)
    {
        return mode.ToLowerInvariant() switch
        {
            "openai" => new OpenAiPlanner(args[0]),
            "gemini" => new GeminiPlanner(args[0]),
            "rule" => new RulePlanner(),
            "command" => new CommandPlanner(),
            _ => new RulePlanner()
        };
    }
}
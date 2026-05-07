using InvariantAgent.Adaptive;
using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Control.Post;
using InvariantAgent.Core.Control.Pre;
using InvariantAgent.Core.Model;
using InvariantAgent.Core.Pipeline;
using InvariantAgent.Execution.Engine;
using InvariantAgent.Safety.Invariants.Action;
using InvariantAgent.Safety.Invariants.Outcome;
using InvariantAgent.Simulation;
using InvariantAgent.Tools;
using InvariantAgent.Tools.BuiltIn;

namespace InvariantAgent.ConsoleApp;

internal static class Program
{
    static void Main()
    {
        Console.WriteLine("=== InvariantAgent REPL ===");
        Console.WriteLine("Commands:");
        Console.WriteLine("  exit      - quit");
        Console.WriteLine("  clear     - clear screen");
        Console.WriteLine();

        var engine = BuildEngine();

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
                Run(engine, input);
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

    private static AgentSimulationEngine BuildEngine()
    {
        // Invariants
        var actionSet = new InvariantSet<AgentAction>(
            new List<IInvariant<AgentAction>>
            {
                new NoEmptyInputInvariant()
            });

        var outcomeSet = new InvariantSet<AgentOutcome>(
            new List<IInvariant<AgentOutcome>>
            {
                new SuccessOutcomeInvariant()
            });

        // Control
        var pre = new PreControl(actionSet);
        var post = new PostControl(outcomeSet);

        // Tools
        var registry = new ToolRegistry(new List<ITool>
        {
            new EchoTool(),
            new SearchTool(),
            new CalculatorTool()
        });

        var executor = new ToolExecutor(registry);

        // Planner
        var planner = new Planner();

        // Engine
        return new AgentSimulationEngine(
            planner,
            pre,
            post,
            executor,
            new StateReducer());
    }

    private static void Run(AgentSimulationEngine engine, string input)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine();
        Console.WriteLine($"INPUT: {input}");
        Console.ResetColor();

        var state = engine.Run(input);

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("FINAL STATE");
        Console.ResetColor();

        Console.WriteLine($"Version: {state.Version}");
        Console.WriteLine($"Events : {state.Events.Count}");
        Console.WriteLine();

        foreach (var e in state.Events)
        {
            Console.ForegroundColor = GetColor(e.Type);

            Console.Write($"[{e.Type}] ");

            Console.ResetColor();

            Console.WriteLine(e.Payload);
        }
    }

    private static ConsoleColor GetColor(string type)
    {
        return type switch
        {
            "InputReceived" => ConsoleColor.Yellow,
            "PlanGenerated" => ConsoleColor.Cyan,
            "ToolExecuted" => ConsoleColor.Green,
            "InvariantViolation" => ConsoleColor.Red,
            _ => ConsoleColor.Gray
        };
    }
}
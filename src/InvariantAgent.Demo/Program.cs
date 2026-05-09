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
                new NoDeleteInvariant()
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
            new CalculatorTool(),
            new ReplayTool()
        });

        var executor = new ToolExecutor(registry);

        // Choose planner
        var planner = CreatePlanner("rule");

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

        //Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Green;
       // Console.WriteLine("FINAL STATE");
        Console.ResetColor();

        //if (execution != null)
       // {
       //     Console.WriteLine(execution.Payload);
       // }

        // Console.WriteLine($"Version: {state.Version}");
        // Console.WriteLine($"Events : {state.Events.Count}");
        Console.WriteLine();

        var lastStepIndex = state.Events
            .FindLastIndex(e => e.Type == "Step");

        if (lastStepIndex >= 0)
        {
            foreach (var e in state.Events.Skip(lastStepIndex))
            {
                Console.ForegroundColor = GetColor(e.Type);
                Console.Write($"[{e.Type}] ");
                Console.ResetColor();
                Console.WriteLine(e.ToObservation());
            }
        }
    }

    private static ConsoleColor GetColor(string type)
    {
        return type switch
        {
            "Step" => ConsoleColor.Cyan,
            "Plan" => ConsoleColor.Cyan,
            "PreControl" => ConsoleColor.Yellow,
            "PostControl" => ConsoleColor.Yellow,
            "Execution" => ConsoleColor.Green,
            "State" => ConsoleColor.Cyan,
            "InvariantViolation" => ConsoleColor.Red,
            _ => ConsoleColor.Gray
        };
    }

    private static IPlanner CreatePlanner(string mode, params string[] args)
    {
        //var mode = Environment.GetEnvironmentVariable("AGENT_PLANNER");

        return mode.ToLowerInvariant() switch
        {
            "openai" => new OpenAiPlanner(args[0]),
                //Environment.GetEnvironmentVariable("OPENAI_API_KEY")),

            "gemini" => new GeminiPlanner(args[0]),

            "rule" => new RulePlanner(),

            _ => new RulePlanner()
        };
    }
}
using InvariantAgent.Adaptive;
using InvariantAgent.Core.Model;
using InvariantAgent.Core.Control.Post;
using InvariantAgent.Core.Control.Pre;
using InvariantAgent.Core.Pipeline;
using InvariantAgent.Execution.Engine;
using InvariantAgent.Safety.Invariants.Action;
using InvariantAgent.Safety.Invariants.Outcome;
using InvariantAgent.Simulation;
using InvariantAgent.Tools;
using InvariantAgent.Tools.BuiltIn;
using InvariantAgent.Core.Abstractions;

class Program
{
    static void Main()
    {
        Console.WriteLine("=== InvariantAgent Simulation Starting ===");

        // 1. Invariants
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

        // 2. Control
        var pre = new PreControl(actionSet);
        var post = new PostControl(outcomeSet);

        // 3. Tools
        var registry = new ToolRegistry(new List<ITool>
        {
            new EchoTool(),
            new SearchTool(),
            new CalculatorTool()
        });

        var executor = new ToolExecutor(registry);

        // 4. Adaptive
        var planner = new Planner();

        // 5. Engine
        var engine = new AgentSimulationEngine(
            planner,
            pre,
            post,
            executor,
            new StateReducer()
        );

        // 6. Run with trace
        RunWithTrace(engine, "hello world");

        Console.WriteLine("=== Simulation Complete ===");
    }

    static void RunWithTrace(AgentSimulationEngine engine, string input)
    {
        Console.WriteLine($"\nINPUT: {input}\n");

        var state = engine.Run(input);

        Console.WriteLine("\n--- FINAL STATE ---");
        Console.WriteLine($"Version: {state.Version}");
        Console.WriteLine($"Events: {state.Events.Count}");

        foreach (var e in state.Events)
        {
            Console.WriteLine($"[{e.Type}] {e.Payload}");
        }
    }
}
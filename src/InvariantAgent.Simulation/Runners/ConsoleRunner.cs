using InvariantAgent.Simulation.Scenarios;

namespace InvariantAgent.Simulation.Runners
{
    public class ConsoleRunner
    {
        private readonly AgentSimulationEngine _engine;

        public ConsoleRunner(AgentSimulationEngine engine)
        {
            _engine = engine;
        }

        public void RunEcho()
        {
            var scenario = new EchoScenario();

            Console.WriteLine($"Running: {scenario.Name}");
            _engine.Run(scenario.Input);

            Dump();
        }

        public void RunFailure()
        {
            var scenario = new ToolFailureScenario();

            Console.WriteLine($"Running: {scenario.Name}");
            _engine.Run(scenario.Input);

            Dump();
        }

        public void RunDrift()
        {
            var scenario = new DriftScenario();

            Console.WriteLine($"Running: {scenario.Name}");

            foreach (var input in scenario.Inputs)
                _engine.Run(input);

            Dump();
        }

        private void Dump()
        {
            Console.WriteLine("\nEVENT TRACE:");
            foreach (var e in _engine.Events)
            {
                Console.WriteLine($"{e.ToObservation()}");
            }
        }
    }
}

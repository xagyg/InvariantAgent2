
namespace InvariantAgent.Simulation.Replay
{
    public class EventReplayer
    {
        public void Replay(IReadOnlyList<AgentEvent> events)
        {
            Console.WriteLine("REPLAYING TRACE:\n");

            foreach (var e in events)
            {
                switch (e.Type)
                {
                    case "PreBlocked":
                        Console.WriteLine($"[PRE BLOCK] {e.ToObservation()}");
                        break;

                    case "PostBlocked":
                        Console.WriteLine($"[POST BLOCK] {e.ToObservation()}");
                        break;

                    case "OutcomeAccepted":
                        Console.WriteLine($"[OK] {e.ToObservation()}");
                        break;

                    default:
                        Console.WriteLine($"[EVENT] {e.Type}");
                        break;
                }
            }
        }
    }
}

using System.Text;
using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model;
using InvariantAgent.Core.Model.Data;

namespace InvariantAgent.Capabilities.Tools
{
    public class ReplayTool : ICapability
    {
        public string Name => "replay";

        public CapabilityResult Execute(CapabilityRequest request, AgentState state)
        {
            if (state.Events.Count == 0)
            {
                return CapabilityResult.Ok(Name, new TextData { Value = "====\nNo events recorded.\n====" });
            }

            int count = state.Events.Count;

            if (int.TryParse(request.Input, out var requested))
            {
                count = Math.Min(requested, state.Events.Count);
            }

            var sb = new StringBuilder();

            sb.AppendLine("\n==== REPLAY START ====");

            foreach (var e in state.Events.TakeLast(count))
            {
                sb.Append('[')
                  .Append(e.Type)
                  .Append("] ")
                  .AppendLine(e.ToObservation());
            }

            sb.Append("==== REPLAY END ====");

            return CapabilityResult.Ok(Name, new TextData { Value = sb.ToString() });
        }
    }
}
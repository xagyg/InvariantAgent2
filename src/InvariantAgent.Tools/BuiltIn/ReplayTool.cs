using System.Text;
using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model;

namespace InvariantAgent.Tools.BuiltIn
{
    public class ReplayTool : ITool
    {
        public string Name => "replay";

        public ToolResult Run(string input, AgentState state)
        {
            if (state.Events.Count == 0)
            {
                return ToolResult.Ok(Name, "====\nNo events recorded.\n====");
            }

            int count = state.Events.Count;

            if (int.TryParse(input, out var requested))
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
                  .AppendLine(e.Payload?.ToString());
            }

            sb.Append("==== REPLAY END ====");

            return ToolResult.Ok(Name, sb.ToString());
        }
    }
}
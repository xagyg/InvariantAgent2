using System.Text;
using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model;
using InvariantAgent.Core.Model.ToolData;

namespace InvariantAgent.Tools.BuiltIn
{
    public class ReplayTool : ITool
    {
        public string Name => "replay";

        public ToolResult Run(string input, AgentState state)
        {
            if (state.Events.Count == 0)
            {
                return ToolResult.Ok(Name, new TextData { Value = "====\nNo events recorded.\n====" });
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
                  .AppendLine(e.ToObservation());
            }

            sb.Append("==== REPLAY END ====");

            return ToolResult.Ok(Name, new TextData { Value = sb.ToString() });
        }
    }
}
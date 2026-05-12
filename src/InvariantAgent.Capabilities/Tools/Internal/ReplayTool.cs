using System.Text;
using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Capability;
using InvariantAgent.Core.Model.Data;

namespace InvariantAgent.Capabilities.Tools.Internal
{
    public sealed class ReplayTool : ICapability
    {
        private readonly ITransitionStore _store;

        public string Name => "replay";

        public ReplayTool(ITransitionStore store)
        {
            _store = store;
        }

        public CapabilityResult Execute(CapabilityRequest request)
        {
            var transitions = _store.GetAll();

            if (transitions.Count == 0)
            {
                return CapabilityResult.Ok(
                    Name,
                    new TextData
                    {
                        Value = "==== REPLAY START ====\nNo transitions recorded.\n==== REPLAY END ===="
                    });
            }

            var count = transitions.Count;

            if (int.TryParse(request.Input, out var requested))
            {
                count = Math.Min(requested, transitions.Count);
            }

            var sb = new StringBuilder();

            sb.AppendLine("\n==== REPLAY START ====");

            foreach (var transition in transitions.TakeLast(count))
            {
                sb.AppendLine($"Transition={transition.Id}");

                var beforeVersion = transition.Before?.Version.ToString() ?? "?";

                var afterVersion = transition.After?.Version.ToString() ?? "not applied";

                sb.AppendLine($"State = {beforeVersion} -> {afterVersion}");

                sb.AppendLine($"Status={transition.Status}");

                if (transition.SelfModification != null)
                {
                    sb.AppendLine(
                        $"SelfModification={transition.SelfModification.Target}.{transition.SelfModification.Operation} {transition.SelfModification.Key}");
                }

                if (!string.IsNullOrWhiteSpace(transition.Reason))
                {
                    sb.AppendLine($"Reason={transition.Reason}");
                }

                foreach (var e in transition.Events)
                {
                    sb.Append('[')
                      .Append(e.Stage)
                      .Append("] ")
                      .AppendLine(e.Message);
                }

                if (transition.After?.Memory.Count > 0)
                {
                    sb.AppendLine("Memory:");

                    foreach (var item in transition.After.Memory)
                    {
                        sb.AppendLine($"  {item.Key}={item.Value}");
                    }
                }

                sb.AppendLine();
            }

            sb.Append("==== REPLAY END ====");

            return CapabilityResult.Ok(
                Name,
                new TextData
                {
                    Value = sb.ToString()
                });
        }
    }
}
using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Control;
using InvariantAgent.Core.Model.Capability;
using InvariantAgent.Core.Model.Control;
using InvariantAgent.Core.Model.Data;

namespace InvariantAgent.Capabilities.Tools.Internal
{
    /// <summary>
    /// <c>context [normal|degraded|emergency|recovery|maintenance] [reason]</c>: shows or sets the HIG-C operational context
    /// (IBAAM 3 revised, Section 5). Operator-only: a planner can never change the context its own conflicts are judged under.
    /// </summary>
    /// <remarks>A new context applies from the next transition; the transition that sets it keeps the old one.</remarks>
    public sealed class ContextTool : ICapability
    {
        private readonly OperationalContextState _state;

        /// <summary>Creates the tool.</summary>
        /// <param name="state">The operational context in force.</param>
        public ContextTool(OperationalContextState state)
        {
            _state = state;
        }

        /// <inheritdoc />
        public string Name => "context";

        /// <inheritdoc />
        public bool OperatorOnly => true;

        /// <inheritdoc />
        public CapabilityResult Execute(CapabilityRequest request)
        {
            var parts = (request.Input ?? "").Split(' ', 2,
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (parts.Length == 0)
            {
                return CapabilityResult.Ok(Name, new TextData
                {
                    Value = $"Operational context: {_state.Current} (since {_state.Since:u}; {_state.Reason}). " +
                            $"Options: {string.Join(", ", Enum.GetNames<OperationalContext>())}."
                });
            }

            if (!Enum.TryParse<OperationalContext>(parts[0], ignoreCase: true, out var context) ||
                !Enum.IsDefined(context))
            {
                return CapabilityResult.Fail(Name,
                    $"Unknown context '{parts[0]}'. Expected one of: {string.Join(", ", Enum.GetNames<OperationalContext>())}.");
            }

            var reason = parts.Length > 1 ? parts[1] : "set by operator";
            var previous = _state.Set(context, reason);

            return CapabilityResult.Ok(Name, new TextData
            {
                Value = $"Operational context {previous} -> {context} ({reason}). Applies from the next transition."
            });
        }
    }
}

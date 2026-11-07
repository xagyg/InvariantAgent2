using InvariantAgent.Core.Model.Control;
using System;

namespace InvariantAgent.Core.Control
{
    /// <summary>
    /// The HIG-C operational context currently in force (IBAAM 4, Section 5): Normal, Degraded, Emergency, Recovery or
    /// Maintenance.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The runtime stamps <see cref="Current"/> onto every transition when it starts, so same-layer conflicts are resolved
    /// under the context that was in force and replay reproduces that decision. Context is governance state, like the
    /// approved drift baseline: it is set by an operator (the <c>context</c> capability, which planners cannot invoke) or by
    /// configuration (<c>--InvariantAgent:OperationalContext=Degraded</c>), never by the agent itself.
    /// </para>
    /// <para>The value lives in memory; on restart it returns to the configured default.</para>
    /// </remarks>
    public sealed class OperationalContextState
    {
        private readonly object _gate = new();
        private OperationalContext _current;
        private string _reason;
        private DateTime _since;

        /// <summary>Creates the state with an initial context.</summary>
        /// <param name="initial">Context in force at start-up.</param>
        /// <param name="reason">Why that context is in force.</param>
        public OperationalContextState(
            OperationalContext initial = OperationalContext.Normal,
            string reason = "default")
        {
            _current = initial;
            _reason = reason;
            _since = DateTime.UtcNow;
        }

        /// <summary>The context currently in force.</summary>
        public OperationalContext Current
        {
            get { lock (_gate) { return _current; } }
        }

        /// <summary>Why the current context was set.</summary>
        public string Reason
        {
            get { lock (_gate) { return _reason; } }
        }

        /// <summary>When the current context was set (UTC).</summary>
        public DateTime Since
        {
            get { lock (_gate) { return _since; } }
        }

        /// <summary>Changes the context in force. Takes effect from the next transition.</summary>
        /// <param name="context">The new context.</param>
        /// <param name="reason">Why it changed (recorded in the operator's transition).</param>
        /// <returns>The context that was replaced.</returns>
        public OperationalContext Set(OperationalContext context, string reason)
        {
            lock (_gate)
            {
                var previous = _current;
                _current = context;
                _reason = string.IsNullOrWhiteSpace(reason) ? "set by operator" : reason.Trim();
                _since = DateTime.UtcNow;
                return previous;
            }
        }
    }
}

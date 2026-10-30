using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Agent;
using InvariantAgent.Core.Model.Drift;

namespace InvariantAgent.Storage
{
    /// <summary>
    /// Durable <see cref="IDriftBaselineStore"/>. Every approval is appended to a JSONL file, so the file is
    /// also the audit history of baseline approvals; the current baseline is the most recent one.
    /// </summary>
    public sealed class FileDriftBaselineStore : IDriftBaselineStore
    {
        private readonly JsonLinesFile<DriftBaseline> _file;
        private readonly object _lock = new();
        private DriftBaseline? _current;

        /// <summary>Opens the store and loads the most recently approved baseline, if any.</summary>
        /// <param name="filePath">Path of the JSONL file.</param>
        public FileDriftBaselineStore(string filePath)
        {
            _file = new JsonLinesFile<DriftBaseline>(filePath);
            _current = _file.ReadAll().LastOrDefault();
        }

        /// <summary>Full path of the backing file.</summary>
        public string FilePath => _file.FilePath;

        /// <inheritdoc />
        public DriftBaseline Current
        {
            get
            {
                lock (_lock)
                {
                    return _current!;
                }
            }
        }

        /// <summary>Every approved baseline, oldest first.</summary>
        public IReadOnlyList<DriftBaseline> History => _file.ReadAll();

        /// <inheritdoc />
        public DriftBaseline Approve(AgentState state, string reason)
        {
            var baseline = new DriftBaseline
            {
                StateVersion = state.Version,
                Reason = reason,
                State = CloneState(state)
            };

            lock (_lock)
            {
                _file.Append(baseline);
                _current = baseline;
            }

            return baseline;
        }

        // Defensive copy so later changes to the live state cannot alter the approved baseline.
        private static AgentState CloneState(AgentState state)
        {
            return new AgentState
            {
                Version = state.Version,
                Mode = state.Mode,
                Goal = state.Goal,
                Policies = state.Policies.ToList(),
                Events = state.Events.ToList(),
                Memory = new Dictionary<string, object>(state.Memory)
            };
        }
    }
}

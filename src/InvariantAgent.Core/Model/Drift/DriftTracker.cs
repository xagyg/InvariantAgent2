using InvariantAgent.Core.Model.Drift;
using System.Collections.Generic;
using System.Linq;

namespace InvariantAgent.Core.Drift
{
    /// <summary>
    /// In-memory log of drift records raised at runtime (e.g. governance drift from replay validation).
    /// Merged with behavioural drift by the drift analyzer.
    /// </summary>
    public sealed class DriftTracker
    {
        private readonly List<DriftRecord> _records = new();

        /// <summary>All recorded drift events.</summary>
        public IReadOnlyList<DriftRecord> Records => _records;

        /// <summary>Records a drift event.</summary>
        /// <param name="record">The drift record to add.</param>
        public void Record(DriftRecord record)
        {
            _records.Add(record);
        }

        /// <summary>Counts records of the given type.</summary>
        /// <param name="type">Drift type to count.</param>
        public int Count(DriftType type)
        {
            return _records.Count(r => r.Type == type);
        }

        /// <summary>Returns the most recent records.</summary>
        /// <param name="count">Maximum number of records to return.</param>
        public IReadOnlyList<DriftRecord> GetRecent(int count)
        {
            return _records.TakeLast(count).ToList();
        }
    }
}
using InvariantAgent.Core.Model.Drift;
using System.Collections.Generic;
using System.Linq;

namespace InvariantAgent.Core.Drift
{
    public sealed class DriftTracker
    {
        private readonly List<DriftRecord> _records = new();

        public IReadOnlyList<DriftRecord> Records => _records;

        public void Record(DriftRecord record)
        {
            _records.Add(record);
        }

        public int Count(DriftType type)
        {
            return _records.Count(r => r.Type == type);
        }

        public IReadOnlyList<DriftRecord> GetRecent(int count)
        {
            return _records.TakeLast(count).ToList();
        }
    }
}
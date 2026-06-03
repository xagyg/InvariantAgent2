using System;
using System.Collections.Generic;

namespace InvariantAgent.Core.Model.Control
{
    public sealed class MetaInvariantResult
    {
        public bool Passed { get; init; }

        public string Reason { get; init; } = "";

        public IReadOnlyDictionary<string, object> Metadata { get; init; }
            = new Dictionary<string, object>();

        public static MetaInvariantResult Allow(
            string reason = "",
            IReadOnlyDictionary<string, object> metadata = null)
        {
            return new MetaInvariantResult
            {
                Passed = true,
                Reason = reason,
                Metadata = metadata ?? new Dictionary<string, object>()
            };
        }

        public static MetaInvariantResult Reject(string reason)
        {
            return new MetaInvariantResult
            {
                Passed = false,
                Reason = reason
            };
        }
    }
}

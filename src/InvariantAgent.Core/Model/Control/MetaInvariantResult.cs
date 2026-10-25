using System;
using System.Collections.Generic;

namespace InvariantAgent.Core.Model.Control
{
    /// <summary>Result of a meta-invariant evaluation, with optional metadata for the override record.</summary>
    public sealed class MetaInvariantResult
    {
        /// <summary><c>true</c> if the meta-invariant allows the override.</summary>
        public bool Passed { get; init; }

        /// <summary>Explanation, typically for a rejection.</summary>
        public string Reason { get; init; } = "";

        /// <summary>
        /// Values contributed to the override record. Well-known keys: <c>PreservedHigherPriorityInvariants</c>,
        /// <c>Justification</c>, <c>Audit</c>, <c>RequiresReview</c>, <c>ReviewReasons</c>.
        /// </summary>
        public IReadOnlyDictionary<string, object> Metadata { get; init; }
            = new Dictionary<string, object>();

        /// <summary>Creates an allowing result.</summary>
        /// <param name="reason">Optional explanation.</param>
        /// <param name="metadata">Optional metadata for the override record.</param>
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

        /// <summary>Creates a rejecting result; the candidate violation stays blocking.</summary>
        /// <param name="reason">Why the override is not permitted.</param>
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

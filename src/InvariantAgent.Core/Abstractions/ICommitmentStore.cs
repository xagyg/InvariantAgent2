using System.Collections.Generic;
using InvariantAgent.Core.Model.Conscience;

namespace InvariantAgent.Core.Abstractions
{
    /// <summary>
    /// Append-only store of conscience commitments (IBAAM 6, Section 6.4.2).
    /// </summary>
    /// <remarks>
    /// Commitments are never edited or deleted. Which ones are live (unexpired, in scope, not superseded by a later
    /// reviewer decision) is computed by <c>CommitmentLedger</c>, so the store is also the audit history.
    /// </remarks>
    public interface ICommitmentStore
    {
        /// <summary>Appends a commitment.</summary>
        /// <param name="commitment">The commitment to record.</param>
        void Append(Commitment commitment);

        /// <summary>Every commitment, oldest first.</summary>
        IReadOnlyList<Commitment> GetAll();
    }
}

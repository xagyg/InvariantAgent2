using System;
using System.Collections.Generic;
using System.Linq;
using InvariantAgent.Core.Model.Conscience;

namespace InvariantAgent.Core.Conscience
{
    /// <summary>
    /// Computes which commitments are live (IBAAM 6, Section 6.4.2): unexpired, in scope, and not superseded by a later
    /// reviewer decision.
    /// </summary>
    /// <remarks>
    /// Reviewer commitments take precedence over the agent's own. A reviewer commitment supersedes every earlier
    /// commitment that it resolves (same source transition) or that concerns the same value and a similar subject.
    /// Agent commitments never supersede anything. Liveness is computed from the append-only store, so nothing is lost.
    /// </remarks>
    public static class CommitmentLedger
    {
        /// <summary>Default similarity at or above which two commitments (or a plan and a commitment) are "about the same thing".</summary>
        public const double DefaultMatchThreshold = 0.6;

        /// <summary>Returns the live commitments, oldest first.</summary>
        /// <param name="all">Every commitment in append order.</param>
        /// <param name="now">Time used for expiry.</param>
        /// <param name="conversationId">Current session.</param>
        /// <param name="userId">Current user.</param>
        /// <param name="matchThreshold">Subject similarity used for supersession.</param>
        public static IReadOnlyList<Commitment> Live(
            IReadOnlyList<Commitment> all,
            DateTimeOffset now,
            string conversationId,
            string userId,
            double matchThreshold = DefaultMatchThreshold)
        {
            var live = new List<Commitment>();

            for (var i = 0; i < all.Count; i++)
            {
                var commitment = all[i];

                if (commitment.Expires.HasValue && commitment.Expires.Value <= now)
                {
                    continue;
                }

                if (!InScope(commitment, conversationId, userId))
                {
                    continue;
                }

                if (IsSuperseded(all, i, matchThreshold))
                {
                    continue;
                }

                live.Add(commitment);
            }

            return live;
        }

        /// <summary>Commitments created from (or resolving) the given transition.</summary>
        public static IReadOnlyList<Commitment> ForTransition(IReadOnlyList<Commitment> all, Guid transitionId)
        {
            return all.Where(c => c.SourceTransition == transitionId || c.Resolves == transitionId).ToList();
        }

        /// <summary><c>true</c> if a reviewer has already resolved the given transition.</summary>
        public static bool IsResolved(IReadOnlyList<Commitment> all, Guid transitionId)
        {
            return all.Any(c => c.Origin == CommitmentOrigin.Reviewer && c.Resolves == transitionId);
        }

        /// <summary>Similarity between a plan and a commitment's subject.</summary>
        public static double Match(Commitment commitment, string capability, string input)
        {
            return PlanSimilarity.Score(commitment.Capability, commitment.Subject, capability, input);
        }

        private static bool InScope(Commitment commitment, string conversationId, string userId)
        {
            switch (commitment.Scope)
            {
                case CommitmentScope.Deployment:
                    return true;
                case CommitmentScope.User:
                    return string.Equals(commitment.UserId, userId, StringComparison.Ordinal);
                case CommitmentScope.Conversation:
                    return string.Equals(commitment.ConversationId, conversationId, StringComparison.Ordinal);
                default:
                    return false;
            }
        }

        // Superseded when a later reviewer commitment resolves it or concerns the same value and a similar subject.
        private static bool IsSuperseded(IReadOnlyList<Commitment> all, int index, double matchThreshold)
        {
            var commitment = all[index];

            for (var j = index + 1; j < all.Count; j++)
            {
                var later = all[j];

                if (later.Origin != CommitmentOrigin.Reviewer)
                {
                    continue;
                }

                if (later.Resolves.HasValue && later.Resolves.Value == commitment.SourceTransition)
                {
                    return true;
                }

                if (string.Equals(later.Value, commitment.Value, StringComparison.OrdinalIgnoreCase) &&
                    PlanSimilarity.Score(later.Capability, later.Subject, commitment.Capability, commitment.Subject) >= matchThreshold)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

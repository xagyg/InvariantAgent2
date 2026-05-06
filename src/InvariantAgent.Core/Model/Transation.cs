using System;

namespace InvariantAgent.Core.Model
{
    public class Transition
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;

        // Input (Iₜ)
        public string Input { get; init; }

        // State snapshot reference (optional lightweight pointer)
        public int EventIndexStart { get; init; }
        public int EventIndexEnd { get; init; }

        // Aₜ
        public AgentAction ProposedAction { get; init; }

        // Π_pre result
        public bool PreAllowed { get; init; }
        public string PreReason { get; init; }

        // Executed action (A'ₜ)
        public AgentAction ExecutedAction { get; init; }

        // Oₜ
        public AgentOutcome Outcome { get; init; }

        // Π_post result
        public bool PostAllowed { get; init; }
        public string PostReason { get; init; }

        // Final classification
        public TransitionStatus Status { get; init; }
    }
}

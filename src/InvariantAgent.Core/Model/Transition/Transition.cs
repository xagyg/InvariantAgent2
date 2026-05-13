using System;
using System.Collections.Generic;
using InvariantAgent.Core.Model.Agent;
using InvariantAgent.Core.Model.SelfModification;

namespace InvariantAgent.Core.Model.Transition
{
    public sealed class Transition
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;

        // Iₜ - external/user input
        public string Input { get; init; } = "";

        // Sₜ - state before this transition
        public AgentState Before { get; init; }

        // Aₜ - proposed action from planner/adaptive layer
        public AgentAction ProposedAction { get; set; }

        // Oₜ - observed execution result
        public AgentOutcome Outcome { get; set; }

        // Sₜ₊₁ - state after reducer applies accepted outcome
        public AgentState After { get; set; }

        // Final transition classification
        public TransitionStatus Status { get; set; } = TransitionStatus.Completed;

        // Optional explanation when rejected/blocked
        public string Reason { get; set; } = "";

        public SelfModificationRequest? SelfModification { get; set; }

        public List<TransitionEvent> Events { get; init; } = new();

        public void AddEvent(TransitionEventStage stage, string message, Dictionary<string, object> metadata = null)
        {
            Events.Add(new TransitionEvent
            {
                Stage = stage,
                Message = message,
                Metadata = metadata ?? new Dictionary<string, object>()
            });
        }
    }
}
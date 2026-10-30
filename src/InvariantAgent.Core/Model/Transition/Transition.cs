using System;
using System.Collections.Generic;
using InvariantAgent.Core.Model.Agent;
using InvariantAgent.Core.Model.Drift;
using InvariantAgent.Core.Model.SelfModification;

namespace InvariantAgent.Core.Model.Transition
{
    /// <summary>
    /// The fundamental unit of governed execution: one input processed through the full lifecycle
    /// (planning, pre-control, execution, post-control, self-modification check, reduction, commit).
    /// </summary>
    /// <remarks>
    /// Formally a transition maps <c>(Sₜ, Iₜ)</c> to <c>Sₜ₊₁</c> via a proposed action <c>Aₜ</c> and an
    /// observed outcome <c>Oₜ</c>. Every transition - completed or rejected - is persisted with its
    /// full event trail for replay, audit and drift analysis.
    /// </remarks>
    public sealed class Transition
    {
        /// <summary>Unique transition identifier.</summary>
        public Guid Id { get; init; } = Guid.NewGuid();
        /// <summary>UTC creation time.</summary>
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;

        /// <summary>Current lifecycle phase. Change only via <c>TransitionPhases.MoveTo</c> to enforce legal moves.</summary>
        public TransitionPhase Phase { get; set; } = TransitionPhase.Created;

        // Iₜ - external/user input
        /// <summary>The input <c>Iₜ</c> that initiated this transition.</summary>
        public string Input { get; init; } = "";

        // Sₜ - state before this transition
        /// <summary>The prior state <c>Sₜ</c>.</summary>
        public AgentState Before { get; init; }

        // Aₜ - proposed action from planner/adaptive layer
        /// <summary>The planner's proposed action <c>Aₜ</c>; executed only if plan invariants pass.</summary>
        public AgentAction ProposedAction { get; set; }

        // Oₜ - observed execution result
        /// <summary>The observed outcome <c>Oₜ</c>; <c>null</c> if rejected before execution.</summary>
        public AgentOutcome Outcome { get; set; }

        // Sₜ₊₁ - state after reducer applies accepted outcome
        /// <summary>The next state <c>Sₜ₊₁</c> proposed by the reducer; committed only if reduction invariants pass. <c>null</c> if reduction was never reached.</summary>
        public AgentState After { get; set; }

        // Final transition classification
        /// <summary>Current or final status of the transition.</summary>
        public TransitionStatus Status { get; set; } = TransitionStatus.Completed;

        // Optional explanation when rejected/blocked
        /// <summary>Governance summary explaining a rejection; empty otherwise.</summary>
        public string Reason { get; set; } = "";

        /// <summary>Self-modification proposed by the executed capability, if any.</summary>
        public SelfModificationRequest? SelfModification { get; set; }

        /// <summary>
        /// The drift baseline in force when reduction invariants were evaluated.
        /// </summary>
        /// <remarks>
        /// Recorded so replay scores behavioural drift against the same baseline the live run used, rather than
        /// whatever baseline is approved at replay time. <c>null</c> means it was not recorded (reduction was never
        /// reached, or the transition predates this field).
        /// </remarks>
        public DriftBaselineSnapshot DriftBaseline { get; set; }

        /// <summary>Ordered, auditable event trail for this transition.</summary>
        public List<TransitionEvent> Events { get; init; } = new();

        /// <summary>Appends an event to the transition's audit trail.</summary>
        /// <param name="stage">Pipeline stage that raised the event.</param>
        /// <param name="message">Human-readable message.</param>
        /// <param name="metadata">Optional structured metadata (used by replay, explain and drift analysis).</param>
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
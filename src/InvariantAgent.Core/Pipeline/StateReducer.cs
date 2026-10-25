using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Transition;
using InvariantAgent.Core.Model.Agent;
using System.Collections.Generic;
using System.Linq;

namespace InvariantAgent.Core.Pipeline
{
    /// <summary>
    /// Default <see cref="IStateReducer"/>: assimilates an approved transition into a new proposed state.
    /// </summary>
    /// <remarks>
    /// <para>The reducer:</para>
    /// <list type="bullet">
    /// <item>runs only in the Reduction phase and never for rejected transitions;</item>
    /// <item>creates a new state with <c>Version + 1</c>, copying prior memory;</item>
    /// <item>records <c>lastOutcome</c> for successful, non-introspective capabilities;</item>
    /// <item>applies an approved <c>memory.set</c> self-modification and records a SelfModification event.</item>
    /// </list>
    /// <para>
    /// The resulting <c>After</c> state is a proposal: the runtime commits it only after reduction
    /// invariants (e.g. the behavioural drift bound) pass.
    /// </para>
    /// </remarks>
    public sealed class StateReducer : IStateReducer
    {
        /// <inheritdoc />
        public void Apply(TransitionContext context)
        {
            var transition = context.Transition;

            // reject if transition in the wrong phase or wrong state
            if (context.Transition.Phase != TransitionPhase.Reduction
                || context.Transition.Status == TransitionStatus.Rejected)
            {
                return;
            }

            var before = transition.Before;

            // Build a fresh state rather than mutating Before, so the prior state stays intact for audit.
            // Note: Mode, Goal and Policies are not carried forward yet.
            var after = new AgentState
            {
                Version = before.Version + 1
            };

            // shallow-copy memory for now
            foreach (var kv in before.Memory)
            {
                after.Memory[kv.Key] = kv.Value;
            }

            if (ShouldStoreLastOutcome(transition))                
            {
                after.Memory["lastOutcome"] = transition.Outcome.Result;
            }

            transition.After = after;

            var modification = transition.SelfModification;

            // Self-modification has already passed SelfModification-scope invariants at this point.
            if (modification != null)
            {
                if (modification.Target == "memory" && modification.Operation == "set")
                {
                    // shallow copied earlier
                    var oldValue = after.Memory.TryGetValue(modification.Key, out var existingValue)
                        ? existingValue : null;

                    after.Memory[modification.Key] = modification.Value;

                    transition.AddEvent(TransitionEventStage.SelfModification, $"Set {modification.Key}",
                        new Dictionary<string, object>
                        {
                            // Later:
                            // ["Target"] = "Policy"
                            // ["Target"] = "Planner"
                            ["Target"] = "Memory",
                            ["Operation"] = modification.Operation,
                            ["Key"] = modification.Key,
                            ["OldValue"] = oldValue,
                            ["NewValue"] = modification.Value
                        });
                }
            }

            transition.Status = TransitionStatus.Completed;
        }

        /// <summary>
        /// Returns <c>true</c> for successful outcomes of capabilities that act on the world.
        /// Introspective tools (explain, drift, replay, memory-show, baseline-*) are excluded so that
        /// observing the agent does not itself change its adaptive state.
        /// </summary>
        private static bool ShouldStoreLastOutcome(Transition transition)
        {
            var capability = transition.ProposedAction.Capability;

            // do not store last outcome for introspection tools
            return transition.Outcome?.Success == true &&
                   ! new[]
                   {
                       "explain",
                       "drift",
                       "replay",
                       "memory-show",
                       "baseline-approve",
                       "baseline-show"
                   }.Contains(capability);
        }
    }
}

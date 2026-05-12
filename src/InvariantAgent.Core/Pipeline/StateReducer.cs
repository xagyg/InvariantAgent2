using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Transition;
using InvariantAgent.Core.Model.Agent;

namespace InvariantAgent.Core.Pipeline
{
    public sealed class StateReducer : IStateReducer
    {
        public void Apply(TransitionContext context)
        {
            var transition = context.Transition;

            var before = transition.Before;

            var after = new AgentState
            {
                Version = before.Version + 1
            };

            // shallow-copy memory for now
            foreach (var kv in before.Memory)
            {
                after.Memory[kv.Key] = kv.Value;
            }

            if (transition.Outcome != null)
            {
                after.Memory["lastOutcome"] = transition.Outcome.Result;
            }

            transition.After = after;

            var modification = transition.SelfModification;

            if (modification != null)
            {
                if (modification.Target == "memory" && modification.Operation == "set")
                {
                    after.Memory[modification.Key] = modification.Value;

                    transition.Record("Memory", $"Set {modification.Key}");
                }
            }

            transition.Status = TransitionStatus.Completed;
        }
    }
}
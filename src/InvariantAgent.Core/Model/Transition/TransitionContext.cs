namespace InvariantAgent.Core.Model.Transition
{
    /// <summary>
    /// The context passed through the pipeline (invariants, executor, reducer) for one transition.
    /// </summary>
    public sealed class TransitionContext
    {
        /// <summary>The transition being governed.</summary>
        public Transition Transition { get; set; }

        /// <summary>HIG-C operational context in effect for this transition (default Normal).</summary>
        public InvariantAgent.Core.Model.Control.OperationalContext OperationalContext { get; set; }
            = InvariantAgent.Core.Model.Control.OperationalContext.Normal;

        /// <summary><c>true</c> if the transition has been rejected.</summary>
        public bool IsRejected => Transition?.Status == TransitionStatus.Rejected;

        /// <summary><c>true</c> if the transition has completed.</summary>
        public bool IsCompleted => Transition?.Status == TransitionStatus.Completed;
    }
}

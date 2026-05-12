namespace InvariantAgent.Core.Model.Transition
{
    public sealed class TransitionContext
    {
        public Transition Transition { get; set; }

        public bool IsRejected => Transition?.Status == TransitionStatus.Rejected;

        public bool IsCompleted => Transition?.Status == TransitionStatus.Completed;
    }
}
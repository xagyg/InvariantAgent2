using InvariantAgent.Core.Model.Control;

namespace InvariantAgent.Core.Model.Transition
{
    /// <summary>
    /// The context passed through the pipeline (invariants, executor, reducer) for one transition.
    /// </summary>
    public sealed class TransitionContext
    {
        private Transition _transition;
        private OperationalContext? _pendingContext;

        /// <summary>The transition being governed.</summary>
        public Transition Transition
        {
            get => _transition;
            set
            {
                _transition = value;

                if (value != null && _pendingContext.HasValue)
                {
                    value.OperationalContext = _pendingContext.Value;
                    _pendingContext = null;
                }
            }
        }

        /// <summary>HIG-C operational context in effect for this transition (default Normal).</summary>
        /// <remarks>
        /// Stored on <see cref="Transition"/> (<see cref="Transition.OperationalContext"/>) so it is persisted with the
        /// transition and replay re-evaluates same-layer conflicts under the context the live run used.
        /// </remarks>
        public OperationalContext OperationalContext
        {
            get => _transition?.OperationalContext ?? _pendingContext ?? OperationalContext.Normal;
            set
            {
                if (_transition != null)
                {
                    _transition.OperationalContext = value;
                }
                else
                {
                    _pendingContext = value;
                }
            }
        }

        /// <summary><c>true</c> if the transition has been rejected.</summary>
        public bool IsRejected => Transition?.Status == TransitionStatus.Rejected;

        /// <summary><c>true</c> if the transition has completed.</summary>
        public bool IsCompleted => Transition?.Status == TransitionStatus.Completed;
    }
}

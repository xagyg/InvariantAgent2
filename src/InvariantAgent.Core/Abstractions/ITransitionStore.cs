using InvariantAgent.Core.Model.Transition;
using System.Collections.Generic;

namespace InvariantAgent.Core.Abstractions
{
    /// <summary>
    /// Persists transitions (both completed and rejected) for replay, auditing and drift analysis.
    /// </summary>
    public interface ITransitionStore
    {
        /// <summary>Appends a finished transition to the store.</summary>
        /// <param name="transition">The completed or rejected transition.</param>
        void Append(Transition transition);

        /// <summary>Returns all stored transitions in the order they were appended.</summary>
        IReadOnlyList<Transition> GetAll();
    }
}
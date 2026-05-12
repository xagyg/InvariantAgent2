using InvariantAgent.Core.Model.Transition;
using System.Collections.Generic;

namespace InvariantAgent.Core.Abstractions
{
    public interface ITransitionStore
    {
        void Append(Transition transition);

        IReadOnlyList<Transition> GetAll();
    }
}
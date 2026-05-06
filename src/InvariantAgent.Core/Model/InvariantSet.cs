using System.Collections.Generic;
using System.Linq;

namespace InvariantAgent.Core.Model
{
    public class InvariantSet<T>
    {
        private readonly IReadOnlyList<IInvariant<T>> _invariants;

        public InvariantSet(IEnumerable<IInvariant<T>> invariants)
        {
            _invariants = invariants.ToList();
        }

        public InvariantResult Evaluate(T input)
        {
            foreach (var invariant in _invariants)
            {
                var result = invariant.Evaluate(input);

                if (!result.IsValid)
                    return result; // short-circuit on first failure
            }

            return InvariantResult.Pass();
        }
    }
}

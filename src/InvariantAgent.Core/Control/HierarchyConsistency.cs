using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Control;
using System;
using System.Collections.Generic;
using System.Linq;

namespace InvariantAgent.Core.Control
{
    /// <summary>
    /// Level–criticality consistency (IBAAM 3 revised, Section 7.3): every invariant below the fundamental level must have
    /// criticality below Catastrophic in every operational context.
    /// </summary>
    /// <remarks>
    /// An invariant whose violation could be catastrophic belongs at the fundamental level. Enforcing this rules out the
    /// case where strict layering lets a low-criticality fundamental violation outrank a catastrophic mission violation.
    /// The check runs when the evaluator is built (i.e. whenever the hierarchy or criticality table changes), not at every
    /// decision.
    /// </remarks>
    public static class HierarchyConsistency
    {
        /// <summary>Returns one message per inconsistency; empty when the hierarchy is consistent.</summary>
        public static IReadOnlyList<string> Check(IEnumerable<IInvariant> invariants)
        {
            var problems = new List<string>();

            foreach (var invariant in invariants.Where(i => i.Layer != InvariantLayer.Fundamental))
            {
                var catastrophicIn = Enum.GetValues<OperationalContext>()
                    .Where(c => invariant.CriticalityIn(c) >= InvariantCriticality.Catastrophic)
                    .ToList();

                if (catastrophicIn.Count > 0)
                {
                    problems.Add(
                        $"{invariant.Name} is at level {invariant.Layer} but has Catastrophic criticality in " +
                        $"{string.Join(", ", catastrophicIn)}; move it to Fundamental or lower its criticality.");
                }
            }

            return problems;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace InvariantAgent.Core.Conscience
{
    /// <summary>
    /// Deterministic similarity between two plans, used to retrieve precedents and to match plans to commitments.
    /// </summary>
    /// <remarks>
    /// <c>0.4 x [same capability] + 0.6 x Jaccard(character trigrams of the inputs)</c>, giving a value in 0..1.
    /// Trigrams make near-identical inputs (<c>password=a</c> and <c>password=b</c>) similar while unrelated inputs to the
    /// same capability (<c>echo hi</c> and <c>echo weather</c>) stay below the default precedent threshold of 0.6.
    /// Deliberately simple and inspectable: every flag cites cases a reviewer can compare by eye.
    /// </remarks>
    public static class PlanSimilarity
    {
        private const double CapabilityWeight = 0.4;
        private const double InputWeight = 0.6;

        /// <summary>Similarity of two plans in 0..1.</summary>
        /// <param name="capabilityA">First capability.</param>
        /// <param name="inputA">First input.</param>
        /// <param name="capabilityB">Second capability.</param>
        /// <param name="inputB">Second input.</param>
        public static double Score(string capabilityA, string inputA, string capabilityB, string inputB)
        {
            var sameCapability = string.Equals(
                Normalise(capabilityA), Normalise(capabilityB), StringComparison.Ordinal) ? 1.0 : 0.0;

            return CapabilityWeight * sameCapability + InputWeight * InputSimilarity(inputA, inputB);
        }

        /// <summary>Jaccard similarity of the character trigrams of two inputs (1 when both are empty).</summary>
        public static double InputSimilarity(string a, string b)
        {
            var na = Normalise(a);
            var nb = Normalise(b);

            if (na.Length == 0 && nb.Length == 0)
            {
                return 1.0;
            }

            if (na == nb)
            {
                return 1.0;
            }

            var ta = Trigrams(na);
            var tb = Trigrams(nb);

            if (ta.Count == 0 || tb.Count == 0)
            {
                return 0.0;
            }

            var intersection = ta.Count(tb.Contains);
            var union = ta.Count + tb.Count - intersection;

            return union == 0 ? 0.0 : (double)intersection / union;
        }

        /// <summary>Lower-cases, trims and collapses whitespace.</summary>
        public static string Normalise(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "";
            }

            return string.Join(" ", value.Trim().ToLowerInvariant()
                .Split((char[])null, StringSplitOptions.RemoveEmptyEntries));
        }

        // Trigrams of the padded string, so short inputs still produce some.
        private static HashSet<string> Trigrams(string value)
        {
            var padded = " " + value + " ";
            var set = new HashSet<string>(StringComparer.Ordinal);

            for (var i = 0; i + 3 <= padded.Length; i++)
            {
                set.Add(padded.Substring(i, 3));
            }

            return set;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace InvariantAgent.Core.Model.Control
{
    /// <summary>
    /// The violation profile P(a, κ) of a candidate action (IBAAM 3 revised, Section 6.1).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Component n(ℓ, k) is the total violation degree of the invariants at level ℓ whose violation has criticality k in
    /// the current context. Components are ordered by level first (Fundamental, Mission, Behavioural, Heuristic) and
    /// criticality second (Catastrophic down to Low), most severe first.
    /// </para>
    /// <para>
    /// Profiles are compared lexicographically: any violation at a higher level outweighs any violation at a lower level,
    /// and within a level a more critical violation outweighs any amount of less critical violation.
    /// </para>
    /// </remarks>
    public sealed class ViolationProfile : IComparable<ViolationProfile>
    {
        /// <summary>Levels in profile order, most authoritative first.</summary>
        public static readonly IReadOnlyList<InvariantLayer> LevelOrder = new[]
        {
            InvariantLayer.Fundamental,
            InvariantLayer.Mission,
            InvariantLayer.Behavioural,
            InvariantLayer.AdaptiveHeuristic
        };

        /// <summary>Criticalities in profile order, most severe first.</summary>
        public static readonly IReadOnlyList<InvariantCriticality> CriticalityOrder = new[]
        {
            InvariantCriticality.Catastrophic,
            InvariantCriticality.Critical,
            InvariantCriticality.High,
            InvariantCriticality.Medium,
            InvariantCriticality.Low
        };

        // Differences smaller than this are treated as equal, so floating-point noise never decides a resolution.
        private const double Tolerance = 1e-9;

        private readonly double[] _mass = new double[LevelOrder.Count * CriticalityOrder.Count];

        /// <summary>The zero profile: no violations.</summary>
        public static ViolationProfile Zero => new();

        /// <summary>Adds a violation of degree <paramref name="degree"/> at the given level and criticality.</summary>
        public void Add(InvariantLayer layer, InvariantCriticality criticality, double degree)
        {
            if (degree <= 0)
            {
                return;
            }

            _mass[IndexOf(layer, criticality)] += degree;
        }

        /// <summary>Violation mass n(ℓ, k).</summary>
        public double this[InvariantLayer layer, InvariantCriticality criticality] => _mass[IndexOf(layer, criticality)];

        /// <summary>The profile as a vector in comparison order.</summary>
        public IReadOnlyList<double> Components => _mass;

        /// <summary><c>true</c> when nothing is violated.</summary>
        public bool IsZero => _mass.All(m => m <= Tolerance);

        /// <summary><c>true</c> when any fundamental invariant is violated (escalation is then required, Section 7.2).</summary>
        public bool HasFundamentalViolation => FundamentalMass > Tolerance;

        /// <summary>Total violation degree at the fundamental level.</summary>
        public double FundamentalMass =>
            CriticalityOrder.Sum(k => this[InvariantLayer.Fundamental, k]);

        /// <inheritdoc />
        public int CompareTo(ViolationProfile other)
        {
            if (other is null)
            {
                return 1;
            }

            for (var i = 0; i < _mass.Length; i++)
            {
                var diff = _mass[i] - other._mass[i];

                if (Math.Abs(diff) > Tolerance)
                {
                    return diff < 0 ? -1 : 1;
                }
            }

            return 0;
        }

        /// <summary>
        /// The first position (level, criticality) at which this profile and <paramref name="other"/> differ, i.e. the
        /// component that decides the comparison. <c>null</c> when the profiles are equal.
        /// </summary>
        public (InvariantLayer Layer, InvariantCriticality Criticality)? DecidingPosition(ViolationProfile other)
        {
            for (var i = 0; i < _mass.Length; i++)
            {
                if (Math.Abs(_mass[i] - other._mass[i]) > Tolerance)
                {
                    return PositionOf(i);
                }
            }

            return null;
        }

        /// <summary>Compact, human-readable form listing only non-zero components, e.g. <c>M/Critical=1; M/High=0.25</c>.</summary>
        public override string ToString()
        {
            var parts = new List<string>();

            for (var i = 0; i < _mass.Length; i++)
            {
                if (_mass[i] > Tolerance)
                {
                    var (layer, criticality) = PositionOf(i);
                    parts.Add($"{Short(layer)}/{criticality}={_mass[i]:0.###}");
                }
            }

            return parts.Count == 0 ? "none" : string.Join("; ", parts);
        }

        /// <summary>One-letter level code used in profile strings: F, M, B, H.</summary>
        public static string Short(InvariantLayer layer) => layer switch
        {
            InvariantLayer.Fundamental => "F",
            InvariantLayer.Mission => "M",
            InvariantLayer.Behavioural => "B",
            _ => "H"
        };

        private static int IndexOf(InvariantLayer layer, InvariantCriticality criticality)
        {
            var l = LevelIndex(layer);
            var k = CriticalityIndex(criticality);

            return l * CriticalityOrder.Count + k;
        }

        private static (InvariantLayer Layer, InvariantCriticality Criticality) PositionOf(int index)
        {
            return (LevelOrder[index / CriticalityOrder.Count], CriticalityOrder[index % CriticalityOrder.Count]);
        }

        private static int LevelIndex(InvariantLayer layer)
        {
            for (var i = 0; i < LevelOrder.Count; i++)
            {
                if (LevelOrder[i] == layer)
                {
                    return i;
                }
            }

            throw new ArgumentOutOfRangeException(nameof(layer), layer, "Unknown governance level.");
        }

        private static int CriticalityIndex(InvariantCriticality criticality)
        {
            for (var i = 0; i < CriticalityOrder.Count; i++)
            {
                if (CriticalityOrder[i] == criticality)
                {
                    return i;
                }
            }

            throw new ArgumentOutOfRangeException(nameof(criticality), criticality, "Unknown criticality.");
        }
    }
}

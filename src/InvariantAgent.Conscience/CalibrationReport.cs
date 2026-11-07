using System.Globalization;
using System.Text;
using InvariantAgent.Core.Model.Conscience;
using InvariantAgent.Core.Model.Transition;

namespace InvariantAgent.Conscience;

/// <summary>One reliability-curve bin.</summary>
/// <param name="Lower">Inclusive lower forecast bound.</param>
/// <param name="Upper">Exclusive upper forecast bound (inclusive for the last bin).</param>
/// <param name="Count">Forecasts in the bin.</param>
/// <param name="MeanForecast">Mean forecast violation risk.</param>
/// <param name="ObservedRate">Fraction actually blocked by the controls.</param>
public sealed record CalibrationBin(double Lower, double Upper, int Count, double MeanForecast, double ObservedRate);

/// <summary>
/// Calibration of the violation-risk component against realised control outcomes (IBAAM 6, Section 8): Brier score and a
/// reliability curve.
/// </summary>
/// <remarks>
/// Only plans the conscience let through to the controls are scored, because only they have a realised outcome. Plans it
/// revised away or stopped have none, which is the label-thinning effect of IBAAM 6, Section 9: the more the conscience
/// intervenes, the less of its own forecast it can check.
/// </remarks>
/// <param name="Count">Number of scored forecasts.</param>
/// <param name="BrierScore">Mean squared error of the forecasts (0 is perfect), or <c>null</c> with no forecasts.</param>
/// <param name="BaseRate">Observed rate of control rejections among scored plans.</param>
/// <param name="Unlabelled">Appraised plans with no realised control outcome (revised away or stopped by the conscience).</param>
/// <param name="Bins">Reliability curve.</param>
public sealed record CalibrationReport(
    int Count,
    double? BrierScore,
    double BaseRate,
    int Unlabelled,
    IReadOnlyList<CalibrationBin> Bins)
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    /// <summary>Computes the report from transition history.</summary>
    /// <param name="transitions">History with recorded appraisals.</param>
    /// <param name="binCount">Number of equal-width reliability bins.</param>
    public static CalibrationReport Compute(IEnumerable<Transition> transitions, int binCount = 5)
    {
        var pairs = new List<(double Forecast, double Outcome)>();
        var unlabelled = 0;

        foreach (var transition in transitions)
        {
            var appraisals = transition.Appraisals ?? new List<ConscienceAppraisalRecord>();
            var plan = appraisals
                .Where(a => a.Point == AppraisalPoint.Plan)
                .LastOrDefault();

            if (plan?.Appraisal == null)
            {
                continue;
            }

            var risk = plan.Appraisal.Parts.FirstOrDefault(p => p.Kind == TensionKind.ViolationRisk);

            if (risk == null || !risk.Applicable)
            {
                continue;
            }

            // Only the final plan that proceeded to the controls has a realised outcome.
            if (plan.Effect != ConscienceEffect.Proceeded)
            {
                unlabelled++;
                continue;
            }

            double? outcome = transition.Status switch
            {
                TransitionStatus.Completed => 0,
                TransitionStatus.Rejected when transition.BlockingInvariants?.Count > 0 => 1,
                _ => null
            };

            if (outcome == null)
            {
                // e.g. passed every control and was then stopped at the answer point.
                if (transition.Status == TransitionStatus.Rejected && appraisals.Any(a => a.Stopped))
                {
                    pairs.Add((risk.Value, 0));
                }
                else
                {
                    unlabelled++;
                }

                continue;
            }

            pairs.Add((risk.Value, outcome.Value));
        }

        if (pairs.Count == 0)
        {
            return new CalibrationReport(0, null, 0, unlabelled, Array.Empty<CalibrationBin>());
        }

        var brier = pairs.Average(p => (p.Forecast - p.Outcome) * (p.Forecast - p.Outcome));
        var bins = new List<CalibrationBin>();

        for (var b = 0; b < binCount; b++)
        {
            var lower = (double)b / binCount;
            var upper = (double)(b + 1) / binCount;
            var last = b == binCount - 1;

            var inBin = pairs
                .Where(p => p.Forecast >= lower && (last ? p.Forecast <= upper : p.Forecast < upper))
                .ToList();

            if (inBin.Count == 0)
            {
                continue;
            }

            bins.Add(new CalibrationBin(lower, upper, inBin.Count,
                inBin.Average(p => p.Forecast), inBin.Average(p => p.Outcome)));
        }

        return new CalibrationReport(pairs.Count, brier, pairs.Average(p => p.Outcome), unlabelled, bins);
    }

    /// <summary>Plain-text rendering.</summary>
    public string Format()
    {
        var sb = new StringBuilder();

        sb.AppendLine("Violation-risk calibration (forecast vs realised control outcome):");

        if (Count == 0)
        {
            sb.AppendLine("  No scored forecasts yet.");
        }
        else
        {
            sb.AppendLine($"  Forecasts scored: {Count}");
            sb.AppendLine($"  Brier score: {BrierScore!.Value.ToString("0.000", Invariant)} (0 = perfect; always forecasting the base rate scores {(BaseRate * (1 - BaseRate)).ToString("0.000", Invariant)})");
            sb.AppendLine($"  Observed rejection rate: {BaseRate.ToString("0.00", Invariant)}");
            sb.AppendLine("  Reliability:");

            foreach (var bin in Bins)
            {
                sb.AppendLine(
                    $"    [{bin.Lower.ToString("0.0", Invariant)}-{bin.Upper.ToString("0.0", Invariant)}] n={bin.Count} " +
                    $"forecast {bin.MeanForecast.ToString("0.00", Invariant)} observed {bin.ObservedRate.ToString("0.00", Invariant)}");
            }
        }

        sb.AppendLine($"  Appraised plans with no realised outcome (revised or stopped): {Unlabelled}");

        return sb.ToString().TrimEnd();
    }
}

using InvariantAgent.Core.Conscience;
using InvariantAgent.Core.Model.Conscience;

namespace InvariantAgent.Conscience;

/// <summary>
/// Tuning for the history-calibrated conscience. Defaults are conservative placeholders; IBAAM 6 (Section 6.2) tunes the
/// thresholds against a human-labelled set, which is the next empirical step.
/// </summary>
public sealed class ConscienceOptions
{
    /// <summary>When false the runtime runs without a conscience (pure IBAAM 1-4 behaviour).</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Whether to run the answer appraisal (after reduction invariants, before commit).</summary>
    public bool AppraiseAnswers { get; set; } = true;

    /// <summary>Maximum number of re-plans a Revise stance may request before it becomes Escalate.</summary>
    public int MaxRevisions { get; set; } = 2;

    /// <summary>Tension at or above which the stance is Revise.</summary>
    public double ReviseThreshold { get; set; } = 0.55;

    /// <summary>Tension at or above which the stance is Escalate.</summary>
    public double EscalateThreshold { get; set; } = 0.75;

    /// <summary>Tension at or above which the stance is Veto.</summary>
    public double VetoThreshold { get; set; } = 0.9;

    /// <summary>Minimum plan similarity for a past transition to count as a precedent.</summary>
    public double MinPrecedentSimilarity { get; set; } = 0.6;

    /// <summary>Maximum number of precedents retrieved per appraisal.</summary>
    public int MaxPrecedents { get; set; } = 10;

    /// <summary>Maximum number of precedents (and commitments) cited in a justification.</summary>
    public int MaxCitations { get; set; } = 3;

    /// <summary>
    /// Pseudo-count pulling violation risk towards the deployment base rate. Larger values need more precedent before the
    /// conscience trusts it.
    /// </summary>
    public double PriorPseudoCount { get; set; } = 1.0;

    /// <summary>
    /// Beta prior (alpha) for the deployment base rate of control rejections. With <see cref="BaseRatePriorBeta"/> the
    /// cold-start base rate is alpha / (alpha + beta) = 0.25, below the Revise threshold, so a new deployment does not
    /// intervene on ordinary plans before it has history.
    /// </summary>
    public double BaseRatePriorAlpha { get; set; } = 1.0;

    /// <summary>Beta prior (beta) for the deployment base rate of control rejections.</summary>
    public double BaseRatePriorBeta { get; set; } = 3.0;

    /// <summary>Weight of violation risk when combining components.</summary>
    public double ViolationRiskWeight { get; set; } = 1.0;

    /// <summary>Weight of forecast drift when combining components.</summary>
    public double ForecastDriftWeight { get; set; } = 0.8;

    /// <summary>
    /// How strongly trend pressure amplifies the plan-specific tension: at full pressure (Critical region, high
    /// confidence) the exponent becomes <c>1 + weight</c>. Trend pressure never raises tension on its own.
    /// </summary>
    public double TrendPressureWeight { get; set; } = 0.5;

    /// <summary>Weight of commitment conflict when combining components.</summary>
    public double CommitmentConflictWeight { get; set; } = 1.0;

    /// <summary>How much an agent's own commitment counts relative to a reviewer's (reviewer = 1).</summary>
    public double AgentCommitmentStrength { get; set; } = 0.8;

    /// <summary>Similarity at or above which a plan matches a commitment.</summary>
    public double CommitmentMatchThreshold { get; set; } = CommitmentLedger.DefaultMatchThreshold;

    /// <summary>Lifetime of the agent's own commitments; <c>null</c> for no expiry. Reviewer commitments never expire.</summary>
    public TimeSpan? AgentCommitmentLifetime { get; set; } = TimeSpan.FromDays(30);

    /// <summary>Scope of commitments created by a Revise stance.</summary>
    public CommitmentScope ReviseCommitmentScope { get; set; } = CommitmentScope.Conversation;

    /// <summary>
    /// Scope of commitments created by Escalate and Veto. User scope lets a trajectory spread across sessions by one user
    /// stay visible (IBAAM 6, Case 1).
    /// </summary>
    public CommitmentScope StopCommitmentScope { get; set; } = CommitmentScope.User;

    /// <summary>Scope of reviewer commitments.</summary>
    public CommitmentScope ReviewerCommitmentScope { get; set; } = CommitmentScope.Deployment;

    /// <summary>Identity used for user-scoped commitments. IA2 is single-user, so this is a fixed id.</summary>
    public string UserId { get; set; } = "local";

    /// <summary>
    /// Capabilities that are never appraised: introspection, and the governance tools a human uses to review the
    /// conscience itself (a conscience must not be able to block its own review).
    /// </summary>
    public ISet<string> ExemptCapabilities { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "explain",
        "drift",
        "replay",
        "memory-show",
        "baseline-approve",
        "baseline-show",
        "conscience",
        "conscience-show",
        "conscience-review"
    };
}

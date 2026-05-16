using InvariantAgent.Core.Model.Control;

namespace InvariantAgent.Core.Model.Drift
{
    public sealed class DriftScore
    {
        public int Score { get; init; }

        public InvariantSeverity Severity { get; init; }

        public string Explanation { get; init; } = "";
    }
}

using InvariantAgent.Core.Model.Control;

namespace InvariantAgent.Core.Control.Post
{
    public class PostControlResult
    {
        // Whether outcome is accepted into system state update
        public bool Accepted { get; init; }

        // Optional transformed version of outcome (safe repair / sanitization)
        public object TransformedOutcome { get; init; }

        // Original outcome reference (for auditability)
        public object OriginalOutcome { get; init; }

        // Reason for rejection or transformation
        public string Reason { get; init; }

        // Severity (useful for observability + drift tracking)
        public ViolationType ViolationType { get; init; }
    }
}

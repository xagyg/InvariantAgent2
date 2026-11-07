namespace InvariantAgent.Conscience;

/// <summary>
/// Identifies the current runtime session for conversation-scoped commitments. A restart starts a new conversation,
/// while user- and deployment-scoped commitments carry over.
/// </summary>
/// <remarks>A separate singleton so tools can read it without depending on the coordinator (which would form a DI cycle).</remarks>
public sealed class ConscienceSession
{
    /// <summary>The conversation id.</summary>
    public string ConversationId { get; } = Guid.NewGuid().ToString("N");
}

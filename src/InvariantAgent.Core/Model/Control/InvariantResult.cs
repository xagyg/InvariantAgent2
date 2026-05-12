namespace InvariantAgent.Core.Model.Control
{
    public sealed class InvariantResult
    {
        public bool Passed { get; init; }

        public string Reason { get; init; } = "";

        public static InvariantResult Allow()
        {
            return new InvariantResult
            {
                Passed = true
            };
        }

        public static InvariantResult Reject(string reason)
        {
            return new InvariantResult
            {
                Passed = false,
                Reason = reason
            };
        }
    }
}
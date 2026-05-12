namespace InvariantAgent.Core.Model.Control
{
    public sealed class ControlDecision
    {
        public bool Allowed { get; init; }

        public string Reason { get; init; } = "";

        public static ControlDecision Allow()
        {
            return new ControlDecision
            {
                Allowed = true
            };
        }

        public static ControlDecision Block(string reason)
        {
            return new ControlDecision
            {
                Allowed = false,
                Reason = reason
            };
        }
    }
}
public class ControlEvent : AgentEvent
{
    public override string Type => "Control";

    public bool Allowed { get; init; }
    public string Reason { get; init; }


    public override string ToObservation()
        => (Allowed ? "Allowed" : $"Blocked by {Reason}");
}
public class StateVersionEvent : AgentEvent
{
    public override string Type => "State";

    public int Version { get; init; }

    public override string ToObservation()
        => $"Version={Version}";
}
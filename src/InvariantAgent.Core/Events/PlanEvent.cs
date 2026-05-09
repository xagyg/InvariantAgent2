public class PlanEvent : AgentEvent
{
    public override string Type => "Plan";

    public string Capability { get; init; }
    public string Input { get; init; }

    public override string ToObservation()
        => $"Capability={Capability}, Input={Input}";
}
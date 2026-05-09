public class PlanEvent : AgentEvent
{
    public override string Type => "Plan";

    public string Tool { get; init; }
    public string Input { get; init; }

    public override string ToObservation()
        => $"Tool={Tool}, Input={Input}";
}
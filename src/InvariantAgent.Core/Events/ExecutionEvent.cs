public class ExecutionEvent : AgentEvent
{
    public override string Type => "Execution";

    public string Capability { get; init; }
    public string Result { get; init; }

    public override string ToObservation()
        => $"Capability={Capability}, Result={Result}";
}
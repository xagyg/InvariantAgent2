public class ExecutionEvent : AgentEvent
{
    public override string Type => "Execution";

    public string Tool { get; init; }
    public string Result { get; init; }

    public override string ToObservation()
        => $"Tool={Tool}, Result={Result}";
}
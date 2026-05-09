using System;

public class StepEvent : AgentEvent
{
    public override string Type => "Step";

    public Guid StepId { get; init; }

    public override string ToObservation()
        => $"StepId={StepId}";
}
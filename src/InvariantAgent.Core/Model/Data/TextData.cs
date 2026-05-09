namespace InvariantAgent.Core.Model.Data;

public class TextData : CapabilityData
{
    public string Value { get; set; } = "";

    public override string ToString() => Value;
}
namespace InvariantAgent.Core.Model.ToolData;

public class TextData : ToolData
{
    public string Value { get; set; } = "";

    public override string ToString() => Value;
}
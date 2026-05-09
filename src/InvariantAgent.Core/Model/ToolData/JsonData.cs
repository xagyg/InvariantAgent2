namespace InvariantAgent.Core.Model.ToolData;

public class JsonData : ToolData
{
    public string Json { get; set; } = "";

    public override string ToString() => Json;
}
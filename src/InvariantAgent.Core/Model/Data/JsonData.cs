namespace InvariantAgent.Core.Model.Data;

public class JsonData : CapabilityData
{
    public string Json { get; set; } = "";

    public override string ToString() => Json;
}
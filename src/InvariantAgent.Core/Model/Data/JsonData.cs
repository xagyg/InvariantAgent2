namespace InvariantAgent.Core.Model.Data;

/// <summary>A JSON string payload.</summary>
public class JsonData : CapabilityData
{
    /// <summary>The raw JSON text.</summary>
    public string Json { get; set; } = "";

    public override string ToString() => Json;
}
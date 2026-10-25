namespace InvariantAgent.Core.Model.Data;

/// <summary>A plain-text payload.</summary>
public class TextData : CapabilityData
{
    /// <summary>The text.</summary>
    public string Value { get; set; } = "";

    public override string ToString() => Value;
}
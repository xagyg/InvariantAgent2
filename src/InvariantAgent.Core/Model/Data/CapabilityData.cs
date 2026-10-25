namespace InvariantAgent.Core.Model.Data;

/// <summary>Base type for typed capability payloads.</summary>
public abstract class CapabilityData
{
    /// <summary>Renders the payload as text; this becomes the transition outcome's <c>Result</c>.</summary>
    public abstract override string ToString();
}
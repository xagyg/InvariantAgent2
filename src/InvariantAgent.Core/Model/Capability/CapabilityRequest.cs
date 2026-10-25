namespace InvariantAgent.Core.Model.Capability;

/// <summary>Input passed to <c>ICapability.Execute</c>.</summary>
public class CapabilityRequest
{
    /// <summary>Raw input text selected by the planner.</summary>
    public string Input { get; set; } = "";
}
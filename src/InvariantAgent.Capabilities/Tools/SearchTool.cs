using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Capability;
using InvariantAgent.Core.Model.Data;

namespace InvariantAgent.Capabilities.Tools;

/// <summary>Stub search capability that returns two placeholder results for the query.</summary>
public class SearchTool : ICapability
{
    /// <inheritdoc />
    public string Name => "search";

    /// <inheritdoc />
    public CapabilityResult Execute(CapabilityRequest request)
    {
        return CapabilityResult.Ok(Name, new ListData {                    
            Rows = new List<string>
                {
                $"Result 1 for '{request.Input}'",
                $"Result 2 for '{request.Input}'"
                }            
        });        
    }
}
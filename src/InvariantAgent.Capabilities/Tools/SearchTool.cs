using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model;
using InvariantAgent.Core.Model.Data;

namespace InvariantAgent.Capabilities.Tools;

public class SearchTool : ICapability
{
    public string Name => "search";

    public CapabilityResult Execute(CapabilityRequest request, AgentState state)
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
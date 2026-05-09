using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model;
using InvariantAgent.Core.Model.Data;

namespace InvariantAgent.Capabilities.Services;

public class ExampleHttpService : ICapability
{
    private static readonly HttpClient _httpClient = new HttpClient();

    public string Name => "http_example";

    public CapabilityResult Execute(CapabilityRequest request, AgentState state)
    {
        try
        {
            // NOTE: fixed URL as requested
            var response = _httpClient
                .GetAsync("http://example.com")
                .GetAwaiter()
                .GetResult();

            var content = response
                .Content
                .ReadAsStringAsync()
                .GetAwaiter()
                .GetResult();

            return new CapabilityResult
            {
                Success = response.IsSuccessStatusCode,
                Data = new TextData { Value = content }
            };
        }
        catch (Exception ex)
        {
            return new CapabilityResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }
}
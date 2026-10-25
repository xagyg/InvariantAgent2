using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model.Capability;
using InvariantAgent.Core.Model.Data;

namespace InvariantAgent.Capabilities.Services;

/// <summary>
/// Example external-service capability that performs an HTTP GET against a fixed URL.
/// </summary>
/// <remarks>
/// Demonstrates wrapping an external service as a capability.
/// </remarks>
public class ExampleHttpService : ICapability
{
    // Shared HttpClient to avoid socket exhaustion.
    private static readonly HttpClient _httpClient = new HttpClient();

    /// <inheritdoc />
    public string Name => "http_example";

    /// <inheritdoc />
    public CapabilityResult Execute(CapabilityRequest request)
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
                Capability = Name,
                Success = response.IsSuccessStatusCode,
                Data = new TextData { Value = content }
            };
        }
        catch (Exception ex)
        {
            return new CapabilityResult
            {
                Capability = Name,
                Success = false,
                Error = ex.Message
            };
        }
    }
}
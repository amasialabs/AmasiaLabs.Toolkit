using Microsoft.OpenApi.Any;

namespace AmasiaLabs.Toolkit.MinimalApi;

public class CustomizedProblem
{
    public string? Type { get; init; }
    
    public string? Title { get; init; }
    
    public string? Detail { get; init; }
    
    public IDictionary<string, IOpenApiAny>? Extensions { get; init; }
}
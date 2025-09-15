using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace AmasiaLabs.Toolkit.MinimalApi.Auth.ApiKey;

/// <summary>
/// Options for API Key authentication.
/// </summary>
public sealed class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    /// <summary>
    /// Where to read the API key from.
    /// </summary>
    public ApiKeyLocation Location { get; set; } = ApiKeyLocation.HeaderOrQuery;

    /// <summary>
    /// Header name for an API key. Default: "X-Api-Key".
    /// </summary>
    public string HeaderName { get; set; } = "X-Api-Key";

    /// <summary>
    /// Query parameter name for an API key. Default: "api_key".
    /// </summary>
    public string QueryParameterName { get; set; } = "api_key";

    /// <summary>
    /// Value for WWW-Authenticate header on 401 responses. Default: "ApiKey".
    /// </summary>
    public string WwwAuthenticateScheme { get; set; } = ApiKeyAuthenticationHandler.SchemeName;

    /// <summary>
    /// Optional claims factory. Receives the subject (if validator provides it) or a fallback string.
    /// When null, adds NameIdentifier claim with the subject (or "api-key").
    /// </summary>
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public Func<string, IEnumerable<Claim>>? ClaimsFactory { get; set; }
}

/// <summary>
/// API key source locations.
/// </summary>
public enum ApiKeyLocation
{
    Header,
    Query,
    HeaderOrQuery
}

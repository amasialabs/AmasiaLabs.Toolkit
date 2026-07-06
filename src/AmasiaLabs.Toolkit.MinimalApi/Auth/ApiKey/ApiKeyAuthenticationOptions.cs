using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace AmasiaLabs.Toolkit.MinimalApi.Auth.ApiKey;

/// <summary>
/// Options for API Key authentication.
/// </summary>
public sealed class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    /// <summary>
    /// Where to read the API key from. Defaults to <see cref="ApiKeyLocation.Header"/>.
    /// Reading keys from the query string is opt-in because query values routinely leak
    /// into server/proxy/CDN access logs, browser history, and the Referer header.
    /// </summary>
    public ApiKeyLocation Location { get; set; } = ApiKeyLocation.Header;

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
    /// <summary>Read the key only from the configured header. Default and recommended.</summary>
    Header,

    /// <summary>
    /// Read the key only from the query string. Not recommended: query strings are
    /// routinely captured in access logs, browser history, and can leak via the Referer
    /// header. Prefer <see cref="Header"/>.
    /// </summary>
    Query,

    /// <summary>
    /// Accept the key from either the header or the query string. Inherits the
    /// query-string exposure risk described on <see cref="Query"/>.
    /// </summary>
    HeaderOrQuery
}

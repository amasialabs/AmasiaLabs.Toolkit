using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace AmasiaLabs.Toolkit.MinimalApi.Auth.Hmac;

/// <summary>
/// Options controlling HMAC response signing.
/// </summary>
public sealed class HmacResponseSigningOptions
{
    /// <summary>
    /// Response header name to store the signature. Default: "X-Signature".
    /// </summary>
    public string HeaderName { get; set; } = "X-Signature";

    /// <summary>
    /// Whether to sign only successful responses (2xx). Default: true.
    /// </summary>
    public bool SuccessOnly { get; set; } = true;

    /// <summary>
    /// Predicate to decide whether to sign a given response. Default: authenticated Hmac identity.
    /// </summary>
    public Func<HttpContext, bool> ShouldSign { get; set; } = static ctx =>
        ctx.User.Identities.Any(i => i.IsAuthenticated && string.Equals(i.AuthenticationType, HmacAuthenticationHandler.SchemeName, StringComparison.Ordinal));

    /// <summary>
    /// Encoding used to convert response body bytes to string for signing. Default: UTF-8.
    /// </summary>
    public Encoding Encoding { get; set; } = Encoding.UTF8;

    /// <summary>
    /// Builds a payload string from HTTP context and response body text.
    /// When null, uses the raw response body text.
    /// </summary>
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public Func<HttpContext, string, Task<string>>? BuildPayload { get; set; }

    /// <summary>
    /// Resolves the client id from the current user. Default: NameIdentifier or "client_id" claim.
    /// </summary>
    public Func<ClaimsPrincipal, string?> ResolveClientId { get; set; } = static user =>
        user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirst("client_id")?.Value;
}

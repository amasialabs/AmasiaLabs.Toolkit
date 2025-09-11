using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace AmasiaLabs.Toolkit.MinimalApi.Auth.Hmac;

/// <summary>
/// Options for configuring HMAC authentication behavior.
/// </summary>
public sealed class HmacAuthenticationOptions : AuthenticationSchemeOptions
{
    /// <summary>
    /// Header name containing the client identifier. Default: "X-Client-Id".
    /// </summary>
    public string ClientIdHeader { get; set; } = "X-Client-Id";

    /// <summary>
    /// Header name containing the signature value. Default: "X-Signature".
    /// </summary>
    public string SignatureHeader { get; set; } = "X-Signature";

    /// <summary>
    /// Value for the WWW-Authenticate header on 401 responses. Default: "Hmac".
    /// </summary>
    public string WwwAuthenticateScheme { get; set; } = HmacAuthenticationHandler.SchemeName;

    /// <summary>
    /// Custom payload builder used for signature validation.
    /// When null, the full request body is used.
    /// </summary>
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public Func<HttpContext, Task<string>>? BuildPayload { get; set; }

    /// <summary>
    /// Optional claims factory. When null, NameIdentifier and "client_id" claims are added.
    /// </summary>
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public Func<string, IEnumerable<Claim>>? ClaimsFactory { get; set; }
}

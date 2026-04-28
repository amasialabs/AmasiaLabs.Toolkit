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
    /// When set, takes precedence over <see cref="PayloadMode"/> for backward compatibility:
    /// callers who wired up a custom <see cref="BuildPayload"/> keep their existing behavior
    /// regardless of mode.
    /// </summary>
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public Func<HttpContext, Task<string>>? BuildPayload { get; set; }

    /// <summary>
    /// Optional claims factory. When null, NameIdentifier and "client_id" claims are added.
    /// </summary>
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public Func<string, IEnumerable<Claim>>? ClaimsFactory { get; set; }

    /// <summary>
    /// Selects the payload format used for signature computation. Default:
    /// <see cref="HmacPayloadMode.BodyOnly"/> — preserves the historical body-only behavior
    /// so existing clients keep working. Set to <see cref="HmacPayloadMode.CanonicalRequest"/>
    /// to opt into method+path+query+timestamp+nonce+body-hash signing.
    ///
    /// Has no effect when <see cref="BuildPayload"/> is set; that hook always wins.
    /// </summary>
    public HmacPayloadMode PayloadMode { get; set; } = HmacPayloadMode.BodyOnly;

    /// <summary>
    /// Header name carrying the request timestamp (RFC 3339 / ISO 8601, e.g.
    /// <c>2026-04-28T14:00:00Z</c>) when <see cref="PayloadMode"/> is
    /// <see cref="HmacPayloadMode.CanonicalRequest"/>. Default: "X-Timestamp".
    /// </summary>
    public string TimestampHeader { get; set; } = "X-Timestamp";

    /// <summary>
    /// Header name carrying the request nonce when <see cref="PayloadMode"/> is
    /// <see cref="HmacPayloadMode.CanonicalRequest"/>. Default: "X-Nonce".
    /// </summary>
    public string NonceHeader { get; set; } = "X-Nonce";

    /// <summary>
    /// Maximum allowed clock skew between client and server when
    /// <see cref="PayloadMode"/> is <see cref="HmacPayloadMode.CanonicalRequest"/>.
    /// Requests outside this window (older or further into the future) are rejected.
    /// Default: 5 minutes.
    /// </summary>
    public TimeSpan AllowedClockSkew { get; set; } = TimeSpan.FromMinutes(5);
}

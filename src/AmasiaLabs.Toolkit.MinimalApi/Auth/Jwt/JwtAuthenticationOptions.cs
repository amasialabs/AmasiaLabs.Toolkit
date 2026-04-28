using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace AmasiaLabs.Toolkit.MinimalApi.Auth.Jwt;

/// <summary>
/// Options for configuring JWT bearer authentication via the toolkit's helpers,
/// covering issuer/audience/key, optional cookie token extraction, and explicit
/// hooks for further customizing <see cref="TokenValidationParameters"/> and
/// <see cref="JwtBearerOptions"/> without losing the toolkit's event composition.
/// </summary>
public sealed class JwtAuthenticationOptions
{
    /// <summary>Expected token issuer.</summary>
    public string Issuer { get; set; } = "";

    /// <summary>Expected token audience.</summary>
    public string Audience { get; set; } = "";

    /// <summary>Symmetric signing key (UTF-8 encoded).</summary>
    public string SigningKey { get; set; } = "";

    /// <summary>
    /// Cookie name to read the bearer token from when the Authorization header is absent.
    /// Defaults to "jc". Set to empty string to disable cookie token extraction.
    /// </summary>
    public string CookieName { get; set; } = "jc";

    /// <summary>
    /// When true (default), the toolkit reads the token from <see cref="CookieName"/>
    /// if no Authorization header is present. Set to false to disable entirely.
    /// </summary>
    public bool ReadTokenFromCookie { get; set; } = true;

    /// <summary>
    /// Optional hook invoked after the toolkit sets default <see cref="TokenValidationParameters"/>,
    /// allowing the caller to tweak validation (e.g., relax lifetime in tests, add custom validators).
    /// </summary>
    public Action<TokenValidationParameters>? ConfigureTokenValidationParameters { get; set; }

    /// <summary>
    /// Optional hook invoked before the toolkit composes its events, allowing the caller to set
    /// arbitrary <see cref="JwtBearerOptions"/> including custom event handlers. The toolkit's
    /// cookie extraction and ProblemDetails 401/403 handlers are <em>composed</em> on top of any
    /// handlers configured here — they will not be lost even if the caller assigns
    /// <c>options.Events = new JwtBearerEvents { ... }</c> wholesale.
    /// </summary>
    public Action<JwtBearerOptions>? ConfigureJwtBearer { get; set; }
}

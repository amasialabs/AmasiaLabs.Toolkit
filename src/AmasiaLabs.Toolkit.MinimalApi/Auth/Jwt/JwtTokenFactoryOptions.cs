using Microsoft.AspNetCore.Http;

namespace AmasiaLabs.Toolkit.MinimalApi.Auth.Jwt;

/// <summary>
/// Options controlling JWT token creation and cookie emission.
/// </summary>
public sealed class JwtTokenFactoryOptions
{
    /// <summary>
    /// Token lifetime. Default: 1 hour.
    /// </summary>
    public TimeSpan TokenLifetime { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Explicit token issuer. If null, uses <c>JwtBearerOptions.TokenValidationParameters.ValidIssuer</c>.
    /// </summary>
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public string? Issuer { get; set; }

    /// <summary>
    /// Explicit token audience. If null, uses <c>JwtBearerOptions.TokenValidationParameters.ValidAudience</c>.
    /// </summary>
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public string? Audience { get; set; }

    /// <summary>
    /// Cookie name for emitting tokens. Default: "jc".
    /// </summary>
    public string CookieName { get; set; } = "jc";

    /// <summary>
    /// Cookie HttpOnly flag. Default: true.
    /// </summary>
    public bool CookieHttpOnly { get; set; } = true;

    /// <summary>
    /// Cookie Secure flag. Default: true.
    /// </summary>
    public bool CookieSecure { get; set; } = true;

    /// <summary>
    /// Cookie SameSite mode. Default: Strict.
    /// </summary>
    public SameSiteMode CookieSameSite { get; set; } = SameSiteMode.Strict;
}


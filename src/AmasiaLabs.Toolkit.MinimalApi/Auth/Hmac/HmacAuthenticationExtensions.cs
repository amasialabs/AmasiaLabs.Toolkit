using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace AmasiaLabs.Toolkit.MinimalApi.Auth.Hmac;

/// <summary>
/// Registration helpers for HMAC authentication.
/// </summary>
public static class HmacAuthenticationExtensions
{
    /// <summary>
    /// Adds HMAC authentication scheme and an "HmacOnly" authorization policy.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="schemeName">Scheme name to register (default: "Hmac").</param>
    /// <param name="setAsDefault">Whether to set this scheme as a default authenticate/challenge scheme.</param>
    /// <param name="configure">Optional additional configuration for <see cref="AuthenticationSchemeOptions"/>.</param>
    /// <returns>The authentication builder.</returns>
    public static AuthenticationBuilder AddHmacAuthentication(
        this IServiceCollection services,
        string schemeName = HmacAuthenticationHandler.SchemeName,
        bool setAsDefault = false,
        Action<HmacAuthenticationOptions>? configure = null)
    {
        var builder = services.AddAuthentication(options =>
        {
            if (!setAsDefault)
                return;
            
            options.DefaultAuthenticateScheme = schemeName;
            options.DefaultChallengeScheme = schemeName;
        });

        builder.AddScheme<HmacAuthenticationOptions, HmacAuthenticationHandler>(schemeName, configure ?? (_ => { }));

        services.AddAuthorization(options =>
        {
            options.AddPolicy(HmacAuthenticationHandler.PolicyName, policy => policy
                .AddAuthenticationSchemes(schemeName)
                .RequireAuthenticatedUser());
        });

        return builder;
    }

    /// <summary>
    /// Adds HMAC authentication scheme to an existing <see cref="AuthenticationBuilder"/>.
    /// </summary>
    public static AuthenticationBuilder AddHmac(this AuthenticationBuilder builder, string schemeName = HmacAuthenticationHandler.SchemeName, Action<HmacAuthenticationOptions>? configure = null)
        => builder.AddScheme<HmacAuthenticationOptions, HmacAuthenticationHandler>(schemeName, configure ?? (_ => { }));
}

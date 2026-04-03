using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace AmasiaLabs.Toolkit.MinimalApi.Auth.ApiKey;

public static class ApiKeyAuthenticationExtensions
{
    /// <summary>
    /// Adds API Key authentication scheme and an "ApiKeyOnly" authorization policy.
    /// </summary>
    public static AuthenticationBuilder AddApiKeyAuthentication(
        this IServiceCollection services,
        string schemeName = ApiKeyAuthenticationHandler.SchemeName,
        bool setAsDefault = false,
        Action<ApiKeyAuthenticationOptions>? configure = null)
    {
        var builder = services.AddAuthentication(options =>
        {
            if (setAsDefault)
            {
                options.DefaultAuthenticateScheme = schemeName;
                options.DefaultChallengeScheme = schemeName;
            }
        });

        builder.AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(schemeName, configure ?? (_ => { }));

        services.AddAuthorizationBuilder()
            .AddPolicy("ApiKeyOnly", policy => policy
                .AddAuthenticationSchemes(schemeName)
                .RequireAuthenticatedUser());

        return builder;
    }

    /// <summary>
    /// Adds an API Key scheme to an existing AuthenticationBuilder.
    /// </summary>
    public static AuthenticationBuilder AddApiKey(this AuthenticationBuilder builder, string schemeName = ApiKeyAuthenticationHandler.SchemeName, Action<ApiKeyAuthenticationOptions>? configure = null)
        => builder.AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(schemeName, configure ?? (_ => { }));
}

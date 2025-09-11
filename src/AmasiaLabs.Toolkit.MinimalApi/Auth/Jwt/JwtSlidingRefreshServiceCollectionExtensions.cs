using Microsoft.Extensions.DependencyInjection;

namespace AmasiaLabs.Toolkit.MinimalApi.Auth.Jwt;

/// <summary>
/// Registration helpers for JWT sliding refresh services.
/// </summary>
public static class JwtSlidingRefreshServiceCollectionExtensions
{
    /// <summary>
    /// Registers sliding refresh services and a custom claims refresh provider.
    /// </summary>
    /// <typeparam name="TClaimsRefreshProvider">Custom implementation that loads up-to-date claims.</typeparam>
    /// <param name="services">Service collection.</param>
    /// <param name="configureTokenFactory">Optional token factory options.</param>
    /// <param name="configureSliding">Optional sliding refresh options.</param>
    public static IServiceCollection AddJwtSlidingRefresh<TClaimsRefreshProvider>(
        this IServiceCollection services,
        Action<JwtTokenFactoryOptions>? configureTokenFactory = null,
        Action<JwtSlidingRefreshOptions>? configureSliding = null)
        where TClaimsRefreshProvider : class, IJwtClaimsRefreshProvider
    {
        services.AddScoped<IJwtClaimsRefreshProvider, TClaimsRefreshProvider>();

        services.AddSingleton<IJwtTokenFactory, DefaultJwtTokenFactory>();
        services.AddSingleton<IJwtCookieWriter, DefaultJwtCookieWriter>();
        services.AddScoped<IJwtSlidingRefresher, DefaultJwtSlidingRefresher>();

        if (configureTokenFactory is not null)
            services.Configure(configureTokenFactory);
        if (configureSliding is not null)
            services.Configure(configureSliding);

        return services;
    }
}


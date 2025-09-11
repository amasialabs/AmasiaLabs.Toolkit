using Microsoft.Extensions.DependencyInjection;

namespace AmasiaLabs.Toolkit.MinimalApi.Auth.Jwt.UsernamePasswordAuth;

/// <summary>
/// Registration helpers for username/password authentication pipeline built from app-provided adapters.
/// </summary>
public static class UsernamePasswordAuthServiceCollectionExtensions
{
    /// <summary>
    /// Registers adapter-based username/password authenticator using provided app types.
    /// </summary>
    /// <typeparam name="TUser">Domain user type.</typeparam>
    /// <typeparam name="TUserProvider">User lookup provider.</typeparam>
    /// <typeparam name="TPasswordVerifier">Password verification provider.</typeparam>
    /// <typeparam name="TClaimsMapper">Claims mapping provider.</typeparam>
    public static IServiceCollection AddUsernamePasswordAuthenticator<TUser, TUserProvider, TPasswordVerifier, TClaimsMapper>(
        this IServiceCollection services)
        where TUserProvider : class, ICredentialsUserProvider<TUser>
        where TPasswordVerifier : class, IPasswordVerifier<TUser>
        where TClaimsMapper : class, IClaimsMapper<TUser>
    {
        services.AddScoped<ICredentialsUserProvider<TUser>, TUserProvider>();
        services.AddScoped<IPasswordVerifier<TUser>, TPasswordVerifier>();
        services.AddScoped<IClaimsMapper<TUser>, TClaimsMapper>();
        services.AddScoped<IUsernamePasswordAuthenticator, DefaultUsernamePasswordAuthenticator<TUser>>();
        return services;
    }
}


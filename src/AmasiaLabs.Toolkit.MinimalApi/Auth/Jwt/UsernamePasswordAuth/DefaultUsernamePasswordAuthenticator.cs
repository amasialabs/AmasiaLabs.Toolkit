using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace AmasiaLabs.Toolkit.MinimalApi.Auth.Jwt.UsernamePasswordAuth;

/// <summary>
/// Generic adapter-based username/password authenticator that delegates
/// user lookup, password verification, and claim mapping to app-provided services.
/// </summary>
/// <typeparam name="TUser">Domain user type.</typeparam>
internal sealed class DefaultUsernamePasswordAuthenticator<TUser>(
    ICredentialsUserProvider<TUser> users,
    IPasswordVerifier<TUser> verifier,
    IClaimsMapper<TUser> claims)
    : IUsernamePasswordAuthenticator
{
    public async Task<IEnumerable<Claim>?> AuthenticateAsync(HttpContext context, string username, string password, CancellationToken cancellationToken = default)
    {
        var user = await users.FindByUsernameAsync(context, username, cancellationToken);
        if (user is null)
            return null;

        var ok = await verifier.VerifyAsync(context, user, password, cancellationToken);
        if (!ok)
            return null;

        return claims.Map(context, user);
    }
}


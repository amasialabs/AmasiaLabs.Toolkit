using Microsoft.AspNetCore.Http;

namespace AmasiaLabs.Toolkit.MinimalApi.Auth.Jwt.UsernamePasswordAuth;

/// <summary>
/// Provides user lookup by username.
/// </summary>
/// <typeparam name="TUser">Domain user type.</typeparam>
public interface ICredentialsUserProvider<TUser>
{
    Task<TUser?> FindByUsernameAsync(HttpContext context, string username, CancellationToken cancellationToken = default);
}


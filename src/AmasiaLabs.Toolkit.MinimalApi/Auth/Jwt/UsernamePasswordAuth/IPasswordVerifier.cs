using Microsoft.AspNetCore.Http;

namespace AmasiaLabs.Toolkit.MinimalApi.Auth.Jwt.UsernamePasswordAuth;

/// <summary>
/// Verifies a plaintext password against a stored representation on the given user.
/// </summary>
/// <typeparam name="TUser">Domain user type.</typeparam>
public interface IPasswordVerifier<in TUser>
{
    Task<bool> VerifyAsync(HttpContext context, TUser user, string password, CancellationToken cancellationToken = default);
}


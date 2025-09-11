using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace AmasiaLabs.Toolkit.MinimalApi.Auth.Jwt;

/// <summary>
/// Authenticates a user by username/password and returns claims for token issuance.
/// Implement in your app to plug domain-specific validation and claim mapping.
/// </summary>
public interface IUsernamePasswordAuthenticator
{
    Task<IEnumerable<Claim>?> AuthenticateAsync(
        HttpContext context,
        string username,
        string password,
        CancellationToken cancellationToken = default);
}


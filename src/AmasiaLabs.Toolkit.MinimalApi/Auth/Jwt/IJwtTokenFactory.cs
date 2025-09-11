using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace AmasiaLabs.Toolkit.MinimalApi.Auth.Jwt;

/// <summary>
/// Creates JWT tokens from claims using configuration present on the active JWT bearer scheme.
/// </summary>
public interface IJwtTokenFactory
{
    /// <summary>
    /// Creates a signed JWT string for the given claims.
    /// </summary>
    /// <param name="claims">Claims to embed into the token.</param>
    /// <param name="bearerOptions">The <see cref="JwtBearerOptions"/> to infer signing key and issuer/audience.</param>
    /// <param name="now">Optional time override for testing.</param>
    string CreateToken(IEnumerable<Claim> claims, JwtBearerOptions bearerOptions, DateTimeOffset? now = null);
}


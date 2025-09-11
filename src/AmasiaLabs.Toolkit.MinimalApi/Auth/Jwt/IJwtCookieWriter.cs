using Microsoft.AspNetCore.Http;

namespace AmasiaLabs.Toolkit.MinimalApi.Auth.Jwt;

/// <summary>
/// Writes JWT value to the configured cookie.
/// </summary>
public interface IJwtCookieWriter
{
    void WriteTokenCookie(HttpContext context, string token);
}


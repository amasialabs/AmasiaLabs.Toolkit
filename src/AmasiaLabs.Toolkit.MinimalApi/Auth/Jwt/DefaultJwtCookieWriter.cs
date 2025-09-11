using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace AmasiaLabs.Toolkit.MinimalApi.Auth.Jwt;

internal sealed class DefaultJwtCookieWriter(IOptions<JwtTokenFactoryOptions> options) : IJwtCookieWriter
{
    private readonly JwtTokenFactoryOptions _opts = options.Value;

    public void WriteTokenCookie(HttpContext context, string token)
    {
        context.Response.Cookies.Append(_opts.CookieName, token, new CookieOptions
        {
            HttpOnly = _opts.CookieHttpOnly,
            Secure = _opts.CookieSecure,
            SameSite = _opts.CookieSameSite,
        });
    }
}


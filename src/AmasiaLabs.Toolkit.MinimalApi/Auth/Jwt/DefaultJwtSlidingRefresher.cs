using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

namespace AmasiaLabs.Toolkit.MinimalApi.Auth.Jwt;

internal sealed class DefaultJwtSlidingRefresher(
    IJwtClaimsRefreshProvider claimsProvider,
    IJwtTokenFactory tokenFactory,
    IJwtCookieWriter cookieWriter,
    IOptions<JwtSlidingRefreshOptions> options)
    : IJwtSlidingRefresher
{
    private readonly JwtSlidingRefreshOptions _opts = options.Value;

    public async Task TryRefreshAsync(TokenValidatedContext context, CancellationToken cancellationToken = default)
    {
        var principal = context.Principal;
        if (principal?.Identity?.IsAuthenticated != true)
            return;

        var exp = GetExpiration(principal);
        if (exp is null)
            return;

        var ttl = exp.Value - DateTimeOffset.UtcNow;
        if (ttl > _opts.RefreshThreshold)
            return; // not close enough to expiry

        var refreshed = await claimsProvider.RefreshClaimsAsync(context.HttpContext, principal, cancellationToken)
                       ?? principal.Claims;

        var token = tokenFactory.CreateToken(refreshed, context.Options);
        cookieWriter.WriteTokenCookie(context.HttpContext, token);
    }

    private static DateTimeOffset? GetExpiration(ClaimsPrincipal principal)
    {
        var expString = principal.FindFirst("exp")?.Value;
        return long.TryParse(expString, out var seconds)
            ? DateTimeOffset.FromUnixTimeSeconds(seconds)
            : null;
    }
}


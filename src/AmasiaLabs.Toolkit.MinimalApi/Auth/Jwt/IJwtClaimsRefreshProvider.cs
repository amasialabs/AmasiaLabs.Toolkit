using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace AmasiaLabs.Toolkit.MinimalApi.Auth.Jwt;

/// <summary>
/// Provides a way to refresh or re-hydrate claims for the current authenticated principal.
/// Implementations can query domain stores to produce up-to-date claims.
/// Return null to reuse existing claims as-is.
/// </summary>
public interface IJwtClaimsRefreshProvider
{
    /// <summary>
    /// Returns a refreshed set of claims for the current user, or null to keep existing claims.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <param name="principal">The validated principal from the incoming JWT.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IEnumerable<Claim>?> RefreshClaimsAsync(
        HttpContext context,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default);
}


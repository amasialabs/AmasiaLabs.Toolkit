using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace AmasiaLabs.Toolkit.MinimalApi.Auth.Jwt;

/// <summary>
/// Performs sliding refresh work during <see cref="JwtBearerEvents.OnTokenValidated"/>.
/// </summary>
public interface IJwtSlidingRefresher
{
    Task TryRefreshAsync(TokenValidatedContext context, CancellationToken cancellationToken = default);
}


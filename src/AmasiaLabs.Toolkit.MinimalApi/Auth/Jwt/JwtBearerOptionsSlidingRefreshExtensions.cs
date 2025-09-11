using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;

namespace AmasiaLabs.Toolkit.MinimalApi.Auth.Jwt;

/// <summary>
/// Configures the JWT bearer handler to perform sliding refresh via DI services.
/// </summary>
public static class JwtBearerOptionsSlidingRefreshExtensions
{
    /// <summary>
    /// Enables sliding refresh by invoking <see cref="IJwtSlidingRefresher"/> during <see cref="JwtBearerEvents.OnTokenValidated"/>.
    /// </summary>
    /// <param name="options">JWT bearer options.</param>
    public static void UseSlidingRefresh(this JwtBearerOptions options)
    {
        var prev = options.Events.OnTokenValidated;
        options.Events.OnTokenValidated = async ctx =>
        {
            await prev(ctx);

            var refresher = ctx.HttpContext.RequestServices.GetRequiredService<IJwtSlidingRefresher>();
            await refresher.TryRefreshAsync(ctx, ctx.HttpContext.RequestAborted);
        };
    }
}

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace AmasiaLabs.Toolkit.MinimalApi.Problems;

public static class TooManyRequestsProblemExtensions
{
    public static IApplicationBuilder UseProblemTooManyRequests(this IApplicationBuilder app)
        => app.UseProblemTooManyRequests(configure: null);

    // ReSharper disable once MemberCanBePrivate.Global
    public static IApplicationBuilder UseProblemTooManyRequests(this IApplicationBuilder app, Action<ProblemDetails>? configure)
    {
        return app.Use(async (ctx, next) =>
        {
            await next();

            if (ctx.Response.HasStarted)
                return;

            if (ctx.Response.StatusCode != StatusCodes.Status429TooManyRequests)
                return;

            var status = StatusCodes.Status429TooManyRequests;
            var pd = new ProblemDetails { Status = status, Title = "Too many requests" };

            configure?.Invoke(pd);

            var pds = ctx.RequestServices.GetRequiredService<IProblemDetailsService>();
            var context = new ProblemDetailsContext
            {
                HttpContext = ctx,
                ProblemDetails = pd
            };
            await pds.WriteAsync(context);
        });
    }
}

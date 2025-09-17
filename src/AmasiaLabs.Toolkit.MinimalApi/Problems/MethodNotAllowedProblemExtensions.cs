using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace AmasiaLabs.Toolkit.MinimalApi.Problems;

public static class MethodNotAllowedProblemExtensions
{
    public static IApplicationBuilder UseProblemMethodNotAllowed(this IApplicationBuilder app)
        => app.UseProblemMethodNotAllowed(configure: null);

    // ReSharper disable once MemberCanBePrivate.Global
    public static IApplicationBuilder UseProblemMethodNotAllowed(this IApplicationBuilder app, Action<ProblemDetails>? configure)
    {
        return app.Use(async (ctx, next) =>
        {
            await next();

            if (ctx.Response.HasStarted)
                return;

            if (ctx.Response.StatusCode != StatusCodes.Status405MethodNotAllowed)
                return;

            var status = StatusCodes.Status405MethodNotAllowed;
            var pd = new ProblemDetails { Status = status, Title = "Method not allowed" };

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

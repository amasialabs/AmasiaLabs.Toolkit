using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace AmasiaLabs.Toolkit.MinimalApi.Problems;

public static class BadRequestProblemExtensions
{
    public static IApplicationBuilder UseProblemBadRequest(this IApplicationBuilder app)
        => app.UseProblemBadRequest(configure: null);

    // ReSharper disable once MemberCanBePrivate.Global
    public static IApplicationBuilder UseProblemBadRequest(this IApplicationBuilder app, Action<ProblemDetails>? configure)
    {
        return app.Use(async (ctx, next) =>
        {
            await next();

            if (ctx.Response.HasStarted)
                return;

            if (ctx.Response.StatusCode != StatusCodes.Status400BadRequest)
                return;

            var status = StatusCodes.Status400BadRequest;
            var pd = new ProblemDetails { Status = status, Title = "Bad request" };

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

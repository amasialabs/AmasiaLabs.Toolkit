using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace AmasiaLabs.Toolkit.MinimalApi.Problems;

public static class ConflictProblemExtensions
{
    public static IApplicationBuilder UseProblemConflict(this IApplicationBuilder app)
        => app.UseProblemConflict(configure: null);

    // ReSharper disable once MemberCanBePrivate.Global
    public static IApplicationBuilder UseProblemConflict(this IApplicationBuilder app, Action<ProblemDetails>? configure)
    {
        return app.Use(async (ctx, next) =>
        {
            await next();

            if (ctx.Response.HasStarted)
                return;

            if (ctx.Response.StatusCode != StatusCodes.Status409Conflict)
                return;

            var status = StatusCodes.Status409Conflict;
            var pd = new ProblemDetails { Status = status, Title = "Conflict" };

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

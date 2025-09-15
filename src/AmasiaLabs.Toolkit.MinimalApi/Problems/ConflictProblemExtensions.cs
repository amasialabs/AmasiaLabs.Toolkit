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

            var opts = ctx.RequestServices.GetRequiredService<ProblemHandlingOptions>();
            var status = StatusCodes.Status409Conflict;

            var pd = new ProblemDetails
            {
                Status = status,
                Title = "Conflict",
                Detail = opts.GetMessage(status),
                Instance = ctx.Request.Path,
                Type = opts.TypeUriFactory(status),
                Extensions = { ["traceId"] = ctx.TraceIdentifier }
            };

            configure?.Invoke(pd);

            ctx.Response.ContentType = "application/problem+json";
            await ctx.Response.WriteAsJsonAsync(pd);
        });
    }
}

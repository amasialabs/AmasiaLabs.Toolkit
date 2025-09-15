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

            var opts = ctx.RequestServices.GetRequiredService<ProblemHandlingOptions>();
            var status = StatusCodes.Status400BadRequest;

            var pd = new ProblemDetails
            {
                Status = status,
                Title = "Bad request",
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

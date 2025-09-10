using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace AmasiaLabs.Toolkit.MinimalApi;

public static class TooManyRequestsProblemExtensions
{
    public static IApplicationBuilder UseProblemTooManyRequests(this IApplicationBuilder app)
    {
        return app.Use(async (ctx, next) =>
        {
            await next();

            if (ctx.Response.HasStarted)
                return;

            if (ctx.Response.StatusCode != StatusCodes.Status429TooManyRequests)
                return;

            var opts = ctx.RequestServices.GetRequiredService<ProblemHandlingOptions>();
            var status = StatusCodes.Status429TooManyRequests;

            var pd = new ProblemDetails
            {
                Status = status,
                Title = "Too many requests",
                Detail = opts.GetMessage(status),
                Instance = ctx.Request.Path,
                Type = opts.TypeUriFactory(status),
                Extensions =
                {
                    ["traceId"] = ctx.TraceIdentifier
                }
            };

            ctx.Response.ContentType = "application/problem+json";
            await ctx.Response.WriteAsJsonAsync(pd);
        });
    }
}


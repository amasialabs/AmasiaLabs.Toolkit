using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace AmasiaLabs.Toolkit.MinimalApi.Problems;

public static class FallbackProblemExtensions
{
    public static IEndpointConventionBuilder MapProblemFallback404(
        this IEndpointRouteBuilder app,
        Action<ProblemDetails>? configure = null)
    {
        return app.MapFallback(async ctx =>
        {
            var opts = ctx.RequestServices.GetRequiredService<ProblemHandlingOptions>();
            var status = StatusCodes.Status404NotFound;

            var pd = new ProblemDetails
            {
                Status = status,
                Title = "Not found",
                Detail = opts.GetMessage(status),
                Instance = ctx.Request.Path,
                Type = opts.TypeUriFactory(status),
                Extensions =
                {
                    ["traceId"] = ctx.TraceIdentifier
                }
            };

            configure?.Invoke(pd);

            ctx.Response.StatusCode = status;
            ctx.Response.ContentType = "application/problem+json";
            await ctx.Response.WriteAsJsonAsync(pd);
        });
    }
}

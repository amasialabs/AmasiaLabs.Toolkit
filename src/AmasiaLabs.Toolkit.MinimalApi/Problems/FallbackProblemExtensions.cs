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
            var status = StatusCodes.Status404NotFound;
            var pd = new ProblemDetails { Status = status, Title = "Not found" };

            configure?.Invoke(pd);

            ctx.Response.StatusCode = status;

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

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace AmasiaLabs.Toolkit.MinimalApi.Problems;

public static class FallbackProblemExtensions
{
    public static IEndpointConventionBuilder MapProblemFallback404(
        this IEndpointRouteBuilder app,
        Action<ProblemDetails>? configure = null)
    {
        return app.MapFallback(ctx =>
            ProblemDetailsWriter.WriteAsync(ctx, StatusCodes.Status404NotFound, "Not found", configure: configure));
    }
}

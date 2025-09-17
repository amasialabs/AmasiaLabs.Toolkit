using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AmasiaLabs.Toolkit.MinimalApi.Problems;

public sealed class GlobalExceptionHandler(
    ProblemHandlingOptions options,
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken token)
    {
        if (options.LogExceptions)
        {
            logger.LogError(ex, "Unhandled exception while processing {Method} {Path}", ctx.Request.Method, ctx.Request.Path);
        }

        var mapped = 
            options.ExceptionMaps.FirstOrDefault(kv => kv.Key.IsInstanceOfType(ex)).Value?.Invoke(ex, ctx)
                     ?? options.Resolve(ex, ctx);

        var problem = new ProblemDetails
        {
            Status = mapped.Status,
            Title = mapped.Title,
            Detail = mapped.Detail
        };

        // Delegate serialization and content negotiation to the built-in ProblemDetails service
        var pdContext = new ProblemDetailsContext
        {
            HttpContext = ctx,
            ProblemDetails = problem,
            Exception = ex
        };

        ctx.Response.StatusCode = mapped.Status;
        await problemDetailsService.WriteAsync(pdContext);
        return true;
    }
}

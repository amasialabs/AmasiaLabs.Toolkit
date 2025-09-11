using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AmasiaLabs.Toolkit.MinimalApi;

public sealed class GlobalExceptionHandler(ProblemHandlingOptions options) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken token)
    {
        var mapped = 
            options.ExceptionMaps.FirstOrDefault(kv => kv.Key.IsInstanceOfType(ex)).Value?.Invoke(ex, ctx)
                     ?? options.Resolve(ex, ctx);

        var problem = new ProblemDetails
        {
            Status = mapped.Status,
            Title = mapped.Title,
            Detail = mapped.Detail ?? (options.IncludeExceptionDetails ? ex.Message : options.GetMessage(mapped.Status)),
            Instance = ctx.Request.Path,
            Type = options.TypeUriFactory(mapped.Status),
            Extensions =
            {
                ["traceId"] = ctx.TraceIdentifier
            }
        };

        ctx.Response.StatusCode = mapped.Status;
        ctx.Response.ContentType = "application/problem+json";
        await ctx.Response.WriteAsJsonAsync(problem, cancellationToken: token);
        return true;
    }
}


using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AmasiaLabs.Toolkit.MinimalApi.Problems;

public sealed partial class GlobalExceptionHandler(
    ProblemHandlingOptions options,
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (options.LogExceptions)
        {
            LogUnhandledException(logger, exception, httpContext.Request.Method, httpContext.Request.Path);
        }

        var mapped =
            options.ExceptionMaps.FirstOrDefault(kv => kv.Key.IsInstanceOfType(exception)).Value?.Invoke(exception, httpContext)
                     ?? ProblemHandlingOptions.Resolve(exception, httpContext);

        var problem = new ProblemDetails
        {
            Status = mapped.Status,
            Title = mapped.Title,
            Detail = mapped.Detail
        };

        // Delegate serialization and content negotiation to the built-in ProblemDetails service
        var pdContext = new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        };

        httpContext.Response.StatusCode = mapped.Status;
        await problemDetailsService.WriteAsync(pdContext);
        return true;
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception while processing {Method} {Path}")]
    private static partial void LogUnhandledException(ILogger logger, Exception exception, string method, PathString path);
}

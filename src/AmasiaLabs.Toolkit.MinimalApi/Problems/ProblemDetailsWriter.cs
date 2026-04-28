using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace AmasiaLabs.Toolkit.MinimalApi.Problems;

/// <summary>
/// Centralizes the "create ProblemDetails, set response status, write via IProblemDetailsService"
/// sequence that the various Problem* extensions and auth handlers all share.
/// </summary>
internal static class ProblemDetailsWriter
{
    public static Task WriteAsync(
        HttpContext httpContext,
        int status,
        string title,
        string? detail = null,
        IDictionary<string, object?>? extensions = null,
        Action<ProblemDetails>? configure = null,
        Exception? exception = null)
    {
        var pd = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
        };

        if (extensions is not null)
        {
            foreach (var kv in extensions)
            {
                pd.Extensions[kv.Key] = kv.Value;
            }
        }

        configure?.Invoke(pd);

        httpContext.Response.StatusCode = status;
        var pds = httpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
        var context = new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = pd,
            Exception = exception,
        };
        return pds.WriteAsync(context).AsTask();
    }
}

internal static class ProblemMiddlewareExtensions
{
    /// <summary>
    /// Adds middleware that catches a specific status code set by an upstream component
    /// and writes an RFC 7807 ProblemDetails response.
    /// </summary>
    public static IApplicationBuilder UseProblemForStatus(
        this IApplicationBuilder app,
        int status,
        string defaultTitle,
        Action<ProblemDetails>? configure)
    {
        return app.Use(async (ctx, next) =>
        {
            await next();

            if (ctx.Response.HasStarted)
                return;

            if (ctx.Response.StatusCode != status)
                return;

            await ProblemDetailsWriter.WriteAsync(ctx, status, defaultTitle, configure: configure);
        });
    }
}

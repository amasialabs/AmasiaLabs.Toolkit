using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace AmasiaLabs.Toolkit.MinimalApi.Problems;

/// <summary>
/// Centralizes the "create ProblemDetails, set response status, write via IProblemDetailsService"
/// sequence that the various Problem* extensions, auth handlers and the exception handler share.
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
        return WriteAsync(pds, context);
    }

    // The framework's own IProblemDetailsService, recognised by the implementation type that the
    // public AddProblemDetails registers for it (the type itself is internal to ASP.NET Core).
    // This rests on two things: AddProblemDetails registers the service by implementation type,
    // not by a factory or an instance; and that service's TryWriteAsync answers false, without
    // writing, when none of its writers accepts the request.
    // ApplicationProblemServiceTests.Framework_Service_Should_Be_Registered_By_Type_And_Decline_Without_Writing
    // pins both. If either changes, that test fails; this field is then null or another type, and
    // the toolkit calls WriteAsync as 1.9.0 did.
    private static readonly Type? FrameworkProblemDetailsServiceType = new ServiceCollection()
        .AddProblemDetails()
        .FirstOrDefault(descriptor => descriptor.ServiceType == typeof(IProblemDetailsService))
        ?.ImplementationType;

    /// <summary>
    /// With the framework's own service: asks its writers, and when none accepts the request (an
    /// Accept header that admits no JSON, with no writer for it), writes the problem as
    /// application/problem+json itself, in the form the framework's default writer gives a
    /// request that accepts JSON. With any other service: calls its WriteAsync, as 1.9.0 did,
    /// and leaves the answer to it.
    /// </summary>
    public static async Task WriteAsync(IProblemDetailsService problemDetailsService, ProblemDetailsContext context)
    {
        if (problemDetailsService.GetType() != FrameworkProblemDetailsServiceType)
        {
            await problemDetailsService.WriteAsync(context);
            return;
        }

        if (!await problemDetailsService.TryWriteAsync(context))
            await JsonFallbackProblemDetailsWriter.WriteJsonAsync(context);
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

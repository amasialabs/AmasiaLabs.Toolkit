using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace AmasiaLabs.Toolkit.MinimalApi;

public static class ProblemResultExtensions
{
    public static IResult BadRequest(this HttpContext ctx, string? detail = null, string? title = null, IDictionary<string, object?>? extensions = null)
        => CreateProblem(ctx, StatusCodes.Status400BadRequest, title ?? "Bad request", detail, extensions);

    public static IResult Conflict(this HttpContext ctx, string? detail = null, string? title = null, IDictionary<string, object?>? extensions = null)
        => CreateProblem(ctx, StatusCodes.Status409Conflict, title ?? "Conflict", detail, extensions);

    public static IResult Unprocessable(this HttpContext ctx, string? detail = null, string? title = null, IDictionary<string, object?>? extensions = null)
        => CreateProblem(ctx, StatusCodes.Status422UnprocessableEntity, title ?? "Unprocessable content", detail, extensions);

    public static IResult TooManyRequests(this HttpContext ctx, string? detail = null, string? title = null, IDictionary<string, object?>? extensions = null)
        => CreateProblem(ctx, StatusCodes.Status429TooManyRequests, title ?? "Too many requests", detail, extensions);

    private static IResult CreateProblem(HttpContext ctx, int status, string title, string? detail, IDictionary<string, object?>? extensions)
    {
        var opts = ctx.RequestServices.GetRequiredService<ProblemHandlingOptions>();
        var ext = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["traceId"] = ctx.TraceIdentifier
        };

        if (extensions != null)
        {
            foreach (var kv in extensions)
                ext[kv.Key] = kv.Value;
        }

        return Results.Problem(
            detail: detail ?? opts.GetMessage(status),
            instance: ctx.Request.Path,
            statusCode: status,
            title: title,
            type: opts.TypeUriFactory(status),
            extensions: ext
        );
    }
}


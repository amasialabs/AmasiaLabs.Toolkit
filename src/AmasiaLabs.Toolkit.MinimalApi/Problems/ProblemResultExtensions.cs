using Microsoft.AspNetCore.Http;

namespace AmasiaLabs.Toolkit.MinimalApi.Problems;

public static class ProblemResultExtensions
{
    public static IResult Unauthorized(this HttpContext ctx, string? detail = null, string? title = null, IDictionary<string, object?>? extensions = null)
        => CreateProblem(ctx, StatusCodes.Status401Unauthorized, title ?? "Unauthorized", detail, extensions);

    public static IResult Forbidden(this HttpContext ctx, string? detail = null, string? title = null, IDictionary<string, object?>? extensions = null)
        => CreateProblem(ctx, StatusCodes.Status403Forbidden, title ?? "Forbidden", detail, extensions);

    public static IResult BadRequest(this HttpContext ctx, string? detail = null, string? title = null, IDictionary<string, object?>? extensions = null)
        => CreateProblem(ctx, StatusCodes.Status400BadRequest, title ?? "Bad request", detail, extensions);

    public static IResult Conflict(this HttpContext ctx, string? detail = null, string? title = null, IDictionary<string, object?>? extensions = null)
        => CreateProblem(ctx, StatusCodes.Status409Conflict, title ?? "Conflict", detail, extensions);

    public static IResult Unprocessable(this HttpContext ctx, string? detail = null, string? title = null, IDictionary<string, object?>? extensions = null)
        => CreateProblem(ctx, StatusCodes.Status422UnprocessableEntity, title ?? "Unprocessable content", detail, extensions);

    public static IResult TooManyRequests(this HttpContext ctx, string? detail = null, string? title = null, IDictionary<string, object?>? extensions = null)
        => CreateProblem(ctx, StatusCodes.Status429TooManyRequests, title ?? "Too many requests", detail, extensions);

    private static ServiceProblemResult CreateProblem(HttpContext ctx, int status, string title, string? detail, IDictionary<string, object?>? extensions)
    {
        return new ServiceProblemResult(
            status: status,
            title: title,
            detail: detail,
            extensions: extensions
        );
    }

    private sealed class ServiceProblemResult(int status, string title, string? detail, IDictionary<string, object?>? extensions)
        : IResult
    {
        public Task ExecuteAsync(HttpContext httpContext)
            => ProblemDetailsWriter.WriteAsync(httpContext, status, title, detail, extensions);
    }
}

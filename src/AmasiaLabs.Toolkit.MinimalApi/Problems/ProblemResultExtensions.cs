using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;

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

    private sealed class ServiceProblemResult : IResult
    {
        private readonly int status;
        private readonly string title;
        private readonly string? detail;
        private readonly IDictionary<string, object?>? extensions;

        public ServiceProblemResult(int status, string title, string? detail, IDictionary<string, object?>? extensions)
        {
            this.status = status;
            this.title = title;
            this.detail = detail;
            this.extensions = extensions;
        }

        public async Task ExecuteAsync(HttpContext httpContext)
        {
            var problem = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = detail
            };

            if (extensions is not null)
            {
                foreach (var kv in extensions)
                {
                    problem.Extensions[kv.Key] = kv.Value;
                }
            }

            httpContext.Response.StatusCode = status;
            var pds = httpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
            var context = new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = problem
            };
            await pds.WriteAsync(context);
        }
    }
}

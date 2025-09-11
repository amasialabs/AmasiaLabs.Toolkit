using Microsoft.AspNetCore.Http;

namespace AmasiaLabs.Toolkit.MinimalApi.Problems;

public sealed class ProblemHandlingOptions
{
    public bool IncludeExceptionDetails { get; set; } = false;

    // ReSharper disable once MemberCanBePrivate.Global
    public Dictionary<int, string> StatusMessages { get; } = new()
    {
        [StatusCodes.Status400BadRequest] = "Request payload is invalid",
        [StatusCodes.Status401Unauthorized] = "Authentication required",
        [StatusCodes.Status403Forbidden] = "Access denied",
        [StatusCodes.Status404NotFound] = "Resource not found",
        [StatusCodes.Status405MethodNotAllowed] = "HTTP method not allowed for this route",
        [StatusCodes.Status409Conflict] = "Conflict with current state",
        [StatusCodes.Status422UnprocessableEntity] = "Validation failed",
        [StatusCodes.Status429TooManyRequests] = "Too many requests",
        [StatusCodes.Status500InternalServerError] = "Internal server error"
    };

    // ReSharper disable once CollectionNeverUpdated.Global
    public Dictionary<Type, Func<Exception, HttpContext, (int Status, string Title, string? Detail)>> ExceptionMaps { get; } = new();

    public Func<int, string> TypeUriFactory { get; set; } = status => $"https://httpstatuses.io/{status}";

    public (int Status, string Title, string? Detail) Resolve(Exception ex, HttpContext ctx) => ex switch
    {
        ArgumentException or FormatException => (StatusCodes.Status400BadRequest, "Bad request", null),
        KeyNotFoundException => (StatusCodes.Status404NotFound, "Not found", null),
        UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "Forbidden", null),
        OperationCanceledException when ctx.RequestAborted.IsCancellationRequested => (499, "Client closed request", null),
        _ => (StatusCodes.Status500InternalServerError, "Internal server error", null)
    };

    public string GetMessage(int status) => StatusMessages.GetValueOrDefault(status, "An error occurred.");
}

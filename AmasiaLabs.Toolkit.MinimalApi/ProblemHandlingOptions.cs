using Microsoft.AspNetCore.Http;

namespace AmasiaLabs.Toolkit.MinimalApi;

public sealed class ProblemHandlingOptions
{
    public bool IncludeExceptionDetails { get; set; } = false;

    // ReSharper disable once MemberCanBePrivate.Global
    public Dictionary<int, string> StatusMessages { get; } = new()
    {
        [StatusCodes.Status400BadRequest] = "Invalid request.",
        [StatusCodes.Status429TooManyRequests] = "Too many requests.",
        [StatusCodes.Status403Forbidden] = "Access denied.",
        [StatusCodes.Status404NotFound] = "Resource not found.",
        [StatusCodes.Status500InternalServerError] = "Internal server error."
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

    public string GetMessage(int status) => StatusMessages.TryGetValue(status, out var m) ? m : "An error occurred.";
}

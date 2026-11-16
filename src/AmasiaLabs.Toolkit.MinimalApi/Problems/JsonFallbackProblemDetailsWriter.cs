using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using HttpJsonOptions = Microsoft.AspNetCore.Http.Json.JsonOptions;

namespace AmasiaLabs.Toolkit.MinimalApi.Problems;

/// <summary>
/// Writes a problem as application/problem+json for a request whose Accept header admits no JSON,
/// which the framework's default writer declines; without a writer that accepts it,
/// IProblemDetailsService.WriteAsync throws. AddGlobalExceptionHandling registers it after the
/// default writer, so a request that accepts JSON is still answered by the default writer, and
/// this writer answers what the framework writes through the service for any other request.
/// A writer registered after this one keeps the requests it can write.
/// <see cref="WriteJsonAsync(ProblemDetailsContext)"/> is the same writing for the toolkit's own
/// problems when the framework's IProblemDetailsService found no writer that accepts them
/// (ProblemDetailsWriter.WriteAsync; an application's own service is left to answer itself).
/// Writing does what the default writer of ASP.NET Core 11 does: the status from the response
/// when the problem has none, the defaults of title and type for the status, traceId from the
/// current activity or else the request's trace identifier, then
/// <see cref="ProblemDetailsOptions.CustomizeProblemDetails"/>; then it writes through the HTTP
/// JSON options and takes the type information from them, so source-generated JSON works. Like
/// the default writer it throws when the response has already started.
/// </summary>
internal sealed class JsonFallbackProblemDetailsWriter(
    IOptions<ProblemDetailsOptions> options,
    IOptions<HttpJsonOptions> jsonOptions,
    IServiceProvider services) : IProblemDetailsWriter
{
    private const string ProblemJsonContentType = "application/problem+json";

    // The defaults of the framework's ProblemDetailsDefaults (internal there), as in
    // Microsoft.AspNetCore.Http.Extensions 11.0.0.
    private static readonly Dictionary<int, (string Type, string Title)> Defaults = new()
    {
        [400] = ("https://tools.ietf.org/html/rfc9110#section-15.5.1", "Bad Request"),
        [401] = ("https://tools.ietf.org/html/rfc9110#section-15.5.2", "Unauthorized"),
        [403] = ("https://tools.ietf.org/html/rfc9110#section-15.5.4", "Forbidden"),
        [404] = ("https://tools.ietf.org/html/rfc9110#section-15.5.5", "Not Found"),
        [405] = ("https://tools.ietf.org/html/rfc9110#section-15.5.6", "Method Not Allowed"),
        [406] = ("https://tools.ietf.org/html/rfc9110#section-15.5.7", "Not Acceptable"),
        [407] = ("https://tools.ietf.org/html/rfc9110#section-15.5.8", "Proxy Authentication Required"),
        [408] = ("https://tools.ietf.org/html/rfc9110#section-15.5.9", "Request Timeout"),
        [409] = ("https://tools.ietf.org/html/rfc9110#section-15.5.10", "Conflict"),
        [410] = ("https://tools.ietf.org/html/rfc9110#section-15.5.11", "Gone"),
        [411] = ("https://tools.ietf.org/html/rfc9110#section-15.5.12", "Length Required"),
        [412] = ("https://tools.ietf.org/html/rfc9110#section-15.5.13", "Precondition Failed"),
        [413] = ("https://tools.ietf.org/html/rfc9110#section-15.5.14", "Content Too Large"),
        [414] = ("https://tools.ietf.org/html/rfc9110#section-15.5.15", "URI Too Long"),
        [415] = ("https://tools.ietf.org/html/rfc9110#section-15.5.16", "Unsupported Media Type"),
        [416] = ("https://tools.ietf.org/html/rfc9110#section-15.5.17", "Range Not Satisfiable"),
        [417] = ("https://tools.ietf.org/html/rfc9110#section-15.5.18", "Expectation Failed"),
        [421] = ("https://tools.ietf.org/html/rfc9110#section-15.5.20", "Misdirected Request"),
        [422] = ("https://tools.ietf.org/html/rfc4918#section-11.2", "Unprocessable Entity"),
        [426] = ("https://tools.ietf.org/html/rfc9110#section-15.5.22", "Upgrade Required"),
        [500] = ("https://tools.ietf.org/html/rfc9110#section-15.6.1", "Internal Server Error"),
        [501] = ("https://tools.ietf.org/html/rfc9110#section-15.6.2", "Not Implemented"),
        [502] = ("https://tools.ietf.org/html/rfc9110#section-15.6.3", "Bad Gateway"),
        [503] = ("https://tools.ietf.org/html/rfc9110#section-15.6.4", "Service Unavailable"),
        [504] = ("https://tools.ietf.org/html/rfc9110#section-15.6.5", "Gateway Timeout"),
        [505] = ("https://tools.ietf.org/html/rfc9110#section-15.6.6", "HTTP Version Not Supported"),
    };

    private readonly ProblemDetailsOptions _options = options.Value;
    private readonly JsonSerializerOptions _serializerOptions = jsonOptions.Value.SerializerOptions;
    private IProblemDetailsWriter[]? _laterWriters;

    public bool CanWrite(ProblemDetailsContext context)
    {
        foreach (var writer in LaterWriters)
        {
            if (writer.CanWrite(context))
                return false;
        }

        return true;
    }

    public ValueTask WriteAsync(ProblemDetailsContext context)
        => WriteJsonAsync(context, _options, _serializerOptions);

    /// <summary>
    /// Writes the problem as application/problem+json with the options of the request's services:
    /// for the toolkit's own problems when no registered writer accepted them.
    /// </summary>
    internal static ValueTask WriteJsonAsync(ProblemDetailsContext context)
    {
        var requestServices = context.HttpContext.RequestServices;
        return WriteJsonAsync(
            context,
            requestServices.GetRequiredService<IOptions<ProblemDetailsOptions>>().Value,
            requestServices.GetRequiredService<IOptions<HttpJsonOptions>>().Value.SerializerOptions);
    }

    private static ValueTask WriteJsonAsync(
        ProblemDetailsContext context,
        ProblemDetailsOptions options,
        JsonSerializerOptions serializerOptions)
    {
        var httpContext = context.HttpContext;
        var problem = context.ProblemDetails;
        ApplyDefaults(problem, httpContext.Response.StatusCode);
        problem.Extensions["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        options.CustomizeProblemDetails?.Invoke(context);

        var typeInfo = serializerOptions.GetTypeInfo(problem.GetType());
        return new ValueTask(httpContext.Response.WriteAsJsonAsync(problem, typeInfo, ProblemJsonContentType));
    }

    // The writers registered after this one, in order; resolved on first use because the writers
    // are resolved together with this one. This writer finds its own place by reference: with a
    // decorated writer, or a container that hands out other instances than the ones it resolved
    // for the service, it does not find itself, sees no later writer, and answers every request
    // that accepts no JSON itself. An application's own writer is safe only when registered
    // before AddGlobalExceptionHandling.
    private IProblemDetailsWriter[] LaterWriters => _laterWriters ??= services
        .GetServices<IProblemDetailsWriter>()
        .SkipWhile(writer => !ReferenceEquals(writer, this))
        .Skip(1)
        .ToArray();

    private static void ApplyDefaults(ProblemDetails problem, int statusCode)
    {
        problem.Status ??= statusCode;
        var status = problem.Status.Value;

        if (Defaults.TryGetValue(status, out var defaults))
        {
            problem.Title ??= defaults.Title;
            problem.Type ??= defaults.Type;
        }
        else if (problem.Title is null)
        {
            var reasonPhrase = ReasonPhrases.GetReasonPhrase(status);
            if (!string.IsNullOrEmpty(reasonPhrase))
                problem.Title = reasonPhrase;
        }
    }
}

using System.Collections.Concurrent;
using System.Net;
using System.Text.RegularExpressions;
using AmasiaLabs.Toolkit.MinimalApi.Problems;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace AmasiaLabs.Toolkit.MinimalApi.Tests.Integration;

/// <summary>
/// With AddGlobalExceptionHandling, a problem is answered as application/problem+json whatever the
/// request's Accept header admits. The bodies expected here are those that 1.9.0 wrote for a
/// request that accepts JSON, captured from it, with the value of traceId masked as "*".
/// </summary>
public partial class ProblemAcceptHeaderTests
{
    internal const string ExceptionMessage = "secret-exception-message";

    internal const string BrowserAccept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";

    internal static readonly string[] Scenarios =
        ["exception", "fallback404", "methodNotAllowed", "conflictHelper", "typedProblem"];

    private static readonly Dictionary<string, (HttpStatusCode Status, string Body)> Expected = new()
    {
        ["exception"] = (HttpStatusCode.InternalServerError,
            """{"type":"https://httpstatuses.io/500","title":"Internal server error","status":500,"detail":"Internal server error","instance":"/throw","traceId":"*"}"""),
        ["fallback404"] = (HttpStatusCode.NotFound,
            """{"type":"https://httpstatuses.io/404","title":"Not found","status":404,"detail":"Resource not found","instance":"/missing","traceId":"*"}"""),
        ["methodNotAllowed"] = (HttpStatusCode.MethodNotAllowed,
            """{"type":"https://httpstatuses.io/405","title":"Method not allowed","status":405,"detail":"HTTP method not allowed for this route","instance":"/only-get","traceId":"*"}"""),
        ["conflictHelper"] = (HttpStatusCode.Conflict,
            """{"type":"https://httpstatuses.io/409","title":"Conflict","status":409,"detail":"Order 42 already exists","instance":"/conflict","orderId":42,"traceId":"*"}"""),
        ["typedProblem"] = (HttpStatusCode.UnprocessableEntity,
            """{"type":"https://httpstatuses.io/422","title":"Unprocessable Entity","status":422,"detail":"Validation failed","instance":"/typed-problem","traceId":"*"}"""),
    };

    public static TheoryData<string, string> NonJsonAccepting => Cross("text/plain", "application/xml", "text/html");

    // "" sends no Accept header.
    public static TheoryData<string, string> JsonAccepting =>
        Cross("", "application/json", "*/*", "application/problem+json", BrowserAccept);

    [Theory]
    [MemberData(nameof(NonJsonAccepting))]
    public async Task Problem_Should_Be_Json_When_Request_Accepts_No_Json(string scenario, string accept)
    {
        // Arrange
        await using var app = await StartAppAsync(scenario);
        var client = app.GetTestClient();

        // Act
        var resp = await SendAsync(client, scenario, accept);
        var body = await resp.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        var (status, expectedBody) = Expected[scenario];
        AssertProblem(resp, body, status, expectedBody);
    }

    [Theory]
    [MemberData(nameof(JsonAccepting))]
    public async Task Problem_Should_Be_Unchanged_When_Request_Accepts_Json(string scenario, string accept)
    {
        // Arrange
        await using var app = await StartAppAsync(scenario);
        var client = app.GetTestClient();

        // Act
        var resp = await SendAsync(client, scenario, accept);
        var body = await resp.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        var (status, expectedBody) = Expected[scenario];
        AssertProblem(resp, body, status, expectedBody);
    }

    [Theory]
    [InlineData("text/plain")]
    [InlineData("application/json")]
    public async Task Own_Writer_Registered_Before_Should_Answer_First(string accept)
    {
        // Arrange
        await using var app = await StartAppAsync(
            "exception",
            before: services => services.AddSingleton<IProblemDetailsWriter, PlainTextWriter>());
        var client = app.GetTestClient();

        // Act
        var resp = await SendAsync(client, "exception", accept);
        var body = await resp.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        resp.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        resp.Content.Headers.ContentType?.ToString().Should().Be("text/plain");
        body.Should().Be("own writer: 500");
    }

    [Fact]
    public async Task Own_Writer_Registered_After_Should_Still_Answer_What_It_Can_Write()
    {
        // Arrange
        await using var app = await StartAppAsync(
            "exception",
            after: services => services.AddSingleton<IProblemDetailsWriter, XmlOnlyWriter>());
        var client = app.GetTestClient();

        // Act
        var xmlResp = await SendAsync(client, "exception", "application/xml");
        var xmlBody = await xmlResp.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var textResp = await SendAsync(client, "exception", "text/plain");
        var textBody = await textResp.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        xmlResp.Content.Headers.ContentType?.ToString().Should().Be("application/xml");
        xmlBody.Should().Be("<problem status=\"500\" />");
        var (status, expectedBody) = Expected["exception"];
        AssertProblem(textResp, textBody, status, expectedBody);
    }

    [Theory]
    [InlineData("text/plain")]
    [InlineData("application/xml")]
    [InlineData("application/json")]
    public async Task Exception_Should_Give_One_Error_Record_Of_The_Toolkit(string accept)
    {
        // Arrange
        var logs = new RecordingLoggerProvider();
        await using var app = await StartAppAsync("exception", logs: logs);
        var client = app.GetTestClient();

        // Act
        HttpResponseMessage? resp = null;
        try
        {
            resp = await SendAsync(client, "exception", accept);
        }
        catch (InvalidOperationException)
        {
            // The test server hands the client an exception that the pipeline rethrew; the
            // records below say why, and the status is checked after them.
        }

        // Assert
        logs.Records.Where(r => r.Level >= LogLevel.Error)
            .Select(r => $"{r.Category}: {r.Message}")
            .Should().Equal($"{typeof(GlobalExceptionHandler).FullName}: Unhandled exception while processing GET /throw");
        logs.Records.Should().NotContain(r => r.Message.Contains("attempting to execute the error handler"));
        resp.Should().NotBeNull();
        resp!.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Writing_After_The_Response_Started_Should_Fail_As_For_A_Request_That_Accepts_Json()
    {
        // Arrange
        await using var app = await StartAppAsync("started");
        var client = app.GetTestClient();

        // Act
        var jsonBody = await (await SendAsync(client, "started", "application/json"))
            .Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var textBody = await (await SendAsync(client, "started", "text/plain"))
            .Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        jsonBody.Should().StartWith("partial;InvalidOperationException: ");
        textBody.Should().Be(jsonBody);
    }

    internal static async Task<WebApplication> StartAppAsync(
        string scenario,
        Action<IServiceCollection>? before = null,
        Action<IServiceCollection>? after = null,
        ILoggerProvider? logs = null)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddLogging();
        if (logs is not null)
        {
            builder.Logging.AddProvider(logs);
        }

        before?.Invoke(builder.Services);
        builder.Services.AddGlobalExceptionHandling();
        after?.Invoke(builder.Services);

        var app = builder.Build();
        app.UseGlobalExceptionHandling();

        switch (scenario)
        {
            case "exception":
                app.MapGet("/throw", string () => throw new InvalidOperationException(ExceptionMessage));
                break;
            case "fallback404":
                app.MapGet("/ok", () => Results.Ok());
                app.MapProblemFallback404();
                break;
            case "methodNotAllowed":
                app.UseProblemMethodNotAllowed();
                app.MapGet("/only-get", () => Results.Ok());
                break;
            case "conflictHelper":
                app.MapGet("/conflict", (HttpContext ctx) => ctx.Conflict(
                    detail: "Order 42 already exists",
                    extensions: new Dictionary<string, object?> { ["orderId"] = 42 }));
                break;
            case "typedProblem":
                app.MapGet("/typed-problem", () => TypedResults.Problem(statusCode: StatusCodes.Status422UnprocessableEntity));
                break;
            case "started":
                // Starts the response, then asks the service to write a problem, and reports
                // what that did after the part already sent.
                app.MapGet("/started", async (HttpContext ctx, IProblemDetailsService problems) =>
                {
                    await ctx.Response.WriteAsync("partial;");
                    await ctx.Response.Body.FlushAsync();
                    string outcome;
                    try
                    {
                        await problems.WriteAsync(new ProblemDetailsContext
                        {
                            HttpContext = ctx,
                            ProblemDetails = new ProblemDetails { Status = StatusCodes.Status500InternalServerError },
                        });
                        outcome = "written";
                    }
                    catch (InvalidOperationException ex)
                    {
                        outcome = $"{ex.GetType().Name}: {ex.Message}";
                    }

                    await ctx.Response.WriteAsync(outcome);
                });
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null);
        }

        await app.StartAsync(TestContext.Current.CancellationToken);
        return app;
    }

    internal static Task<HttpResponseMessage> SendAsync(HttpClient client, string scenario, string accept)
    {
        var request = scenario switch
        {
            "exception" => new HttpRequestMessage(HttpMethod.Get, "/throw"),
            "fallback404" => new HttpRequestMessage(HttpMethod.Get, "/missing"),
            "methodNotAllowed" => new HttpRequestMessage(HttpMethod.Post, "/only-get"),
            "conflictHelper" => new HttpRequestMessage(HttpMethod.Get, "/conflict"),
            "typedProblem" => new HttpRequestMessage(HttpMethod.Get, "/typed-problem"),
            "apiKeyChallenge" => new HttpRequestMessage(HttpMethod.Get, "/sec"),
            "started" => new HttpRequestMessage(HttpMethod.Get, "/started"),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null),
        };

        if (accept.Length > 0)
        {
            // Sent as written, so that a header with several values reaches the server unchanged.
            request.Headers.TryAddWithoutValidation("Accept", accept);
        }

        return client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// The status, the whole Content-Type header, and the raw body with the value of traceId
    /// masked as "*"; the traceId must have a value.
    /// </summary>
    internal static void AssertProblem(HttpResponseMessage resp, string body, HttpStatusCode status, string expectedMaskedBody)
    {
        resp.StatusCode.Should().Be(status, "body: {0}", body);
        resp.Content.Headers.ContentType?.ToString().Should().Be("application/problem+json");
        body.Should().NotContain(ExceptionMessage);
        TraceIdWithValue().IsMatch(body).Should().BeTrue("the body should have a traceId with a value: {0}", body);
        TraceId().Replace(body, "\"traceId\":\"*\"").Should().Be(expectedMaskedBody);
    }

    [GeneratedRegex("\"traceId\":\"[^\"]*\"")]
    private static partial Regex TraceId();

    [GeneratedRegex("\"traceId\":\"[^\"]+\"")]
    private static partial Regex TraceIdWithValue();

    private static TheoryData<string, string> Cross(params string[] accepts)
    {
        var data = new TheoryData<string, string>();
        foreach (var scenario in Scenarios)
        {
            foreach (var accept in accepts)
            {
                data.Add(scenario, accept);
            }
        }

        return data;
    }

    private sealed class PlainTextWriter : IProblemDetailsWriter
    {
        public bool CanWrite(ProblemDetailsContext context) => true;

        public ValueTask WriteAsync(ProblemDetailsContext context)
        {
            context.HttpContext.Response.ContentType = "text/plain";
            return new ValueTask(context.HttpContext.Response.WriteAsync($"own writer: {context.ProblemDetails.Status}"));
        }
    }

    private sealed class XmlOnlyWriter : IProblemDetailsWriter
    {
        public bool CanWrite(ProblemDetailsContext context)
            => context.HttpContext.Request.Headers.Accept.ToString().Contains("application/xml", StringComparison.Ordinal);

        public ValueTask WriteAsync(ProblemDetailsContext context)
        {
            context.HttpContext.Response.ContentType = "application/xml";
            return new ValueTask(context.HttpContext.Response.WriteAsync($"<problem status=\"{context.ProblemDetails.Status}\" />"));
        }
    }

    internal sealed class RecordingLoggerProvider : ILoggerProvider
    {
        public ConcurrentQueue<(string Category, LogLevel Level, string Message)> Records { get; } = new();

        public ILogger CreateLogger(string categoryName) => new RecordingLogger(categoryName, Records);

        public void Dispose()
        {
        }

        private sealed class RecordingLogger(
            string category,
            ConcurrentQueue<(string Category, LogLevel Level, string Message)> records) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
                => records.Enqueue((category, logLevel, formatter(state, exception)));
        }
    }
}

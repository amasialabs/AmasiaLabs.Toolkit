using System.Net;
using AmasiaLabs.Toolkit.MinimalApi.Auth.ApiKey;
using AmasiaLabs.Toolkit.MinimalApi.Problems;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AmasiaLabs.Toolkit.MinimalApi.Tests.Integration;

/// <summary>
/// The toolkit's own problems in the set-up its README allows besides AddGlobalExceptionHandling:
/// only AddProblemDetails(), which registers no fallback writer. A request that accepts no JSON
/// still gets application/problem+json, with the framework's defaults, and the application's
/// customization when it has one, run once. A request that accepts JSON gets the same body from
/// the framework's writer. With nothing registered, the toolkit throws as before.
/// </summary>
public class ProblemWithoutGlobalHandlingTests
{
    private const string NoServiceMessage =
        "No service for type 'Microsoft.AspNetCore.Http.IProblemDetailsService' has been registered.";

    private static readonly string[] Paths = ["conflictHelper", "fallback404", "methodNotAllowed", "apiKeyChallenge"];

    // With AddProblemDetails() alone; traceId masked as "*".
    private static readonly Dictionary<string, (HttpStatusCode Status, string Body)> Expected = new()
    {
        ["conflictHelper"] = (HttpStatusCode.Conflict,
            """{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.10","title":"Conflict","status":409,"detail":"Order 42 already exists","orderId":42,"traceId":"*"}"""),
        ["fallback404"] = (HttpStatusCode.NotFound,
            """{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.5","title":"Not found","status":404,"traceId":"*"}"""),
        ["methodNotAllowed"] = (HttpStatusCode.MethodNotAllowed,
            """{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.6","title":"Method not allowed","status":405,"traceId":"*"}"""),
        ["apiKeyChallenge"] = (HttpStatusCode.Unauthorized,
            """{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.2","title":"Unauthorized","status":401,"traceId":"*"}"""),
    };

    // The first three accept no JSON; "" sends no Accept header.
    public static TheoryData<string, string> Cases => Cross(Paths, "text/plain", "application/xml", "text/html", "", "application/json");

    public static TheoryData<string, string> NothingRegisteredCases => Cross(Paths, "text/plain", "application/json");

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task Problem_Should_Have_The_Framework_Defaults_With_AddProblemDetails_Alone(string path, string accept)
    {
        // Arrange
        await using var app = await StartAppAsync(path, services => services.AddProblemDetails());
        var client = app.GetTestClient();

        // Act
        var resp = await ProblemAcceptHeaderTests.SendAsync(client, path, accept);
        var body = await resp.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        var (status, expectedBody) = Expected[path];
        ProblemAcceptHeaderTests.AssertProblem(resp, body, status, expectedBody);
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task Problem_Should_Have_The_Customization_Once_With_AddProblemDetails_Alone(string path, string accept)
    {
        // Arrange
        await using var app = await StartAppAsync(path, services => services.AddProblemDetails(o =>
            o.CustomizeProblemDetails = ctx =>
            {
                var runs = ctx.ProblemDetails.Extensions.TryGetValue("customized", out var value) && value is int n ? n : 0;
                ctx.ProblemDetails.Extensions["customized"] = runs + 1;
            }));
        var client = app.GetTestClient();

        // Act
        var resp = await ProblemAcceptHeaderTests.SendAsync(client, path, accept);
        var body = await resp.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        var (status, expectedBody) = Expected[path];
        ProblemAcceptHeaderTests.AssertProblem(resp, body, status, expectedBody[..^1] + ",\"customized\":1}");
    }

    [Theory]
    [MemberData(nameof(NothingRegisteredCases))]
    public async Task Problem_Should_Throw_As_Before_When_Nothing_Is_Registered(string path, string accept)
    {
        // Arrange
        await using var app = await StartAppAsync(path, _ => { });
        var client = app.GetTestClient();

        // Act
        var act = () => ProblemAcceptHeaderTests.SendAsync(client, path, accept);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage(NoServiceMessage);
    }

    private static async Task<WebApplication> StartAppAsync(string path, Action<IServiceCollection> registerProblems)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddLogging();
        registerProblems(builder.Services);

        if (path == "apiKeyChallenge")
        {
            builder.Services.AddSingleton<IApiKeyProvider, TestApiKeyProvider>();
            builder.Services.AddApiKeyAuthentication(setAsDefault: true);
            builder.Services.AddAuthorization();
        }

        var app = builder.Build();

        switch (path)
        {
            case "conflictHelper":
                app.MapGet("/conflict", (HttpContext ctx) => ctx.Conflict(
                    detail: "Order 42 already exists",
                    extensions: new Dictionary<string, object?> { ["orderId"] = 42 }));
                break;
            case "fallback404":
                app.MapGet("/ok", () => Results.Ok());
                app.MapProblemFallback404();
                break;
            case "methodNotAllowed":
                app.UseProblemMethodNotAllowed();
                app.MapGet("/only-get", () => Results.Ok());
                break;
            case "apiKeyChallenge":
                app.UseAuthentication();
                app.UseAuthorization();
                app.MapGet("/sec", () => Results.Ok()).RequireAuthorization("ApiKeyOnly");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(path), path, null);
        }

        await app.StartAsync(TestContext.Current.CancellationToken);
        return app;
    }

    private static TheoryData<string, string> Cross(string[] paths, params string[] accepts)
    {
        var data = new TheoryData<string, string>();
        foreach (var path in paths)
        {
            foreach (var accept in accepts)
            {
                data.Add(path, accept);
            }
        }

        return data;
    }

    private sealed class TestApiKeyProvider : IApiKeyProvider
    {
        public Task<string?> GetSubjectAsync(string apiKey, CancellationToken cancellationToken = default)
            => Task.FromResult<string?>(null);
    }
}

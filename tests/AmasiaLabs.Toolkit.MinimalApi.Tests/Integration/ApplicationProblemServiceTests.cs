using System.Net;
using AmasiaLabs.Toolkit.MinimalApi.Problems;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AmasiaLabs.Toolkit.MinimalApi.Tests.Integration;

/// <summary>
/// The toolkit writes a problem itself, when no writer accepted it, only with the framework's own
/// IProblemDetailsService. An application's own service is called through WriteAsync exactly as
/// 1.9.0 called it, and what it writes is the answer. The answers expected here are those of
/// 1.9.0, captured from it.
/// </summary>
public class ApplicationProblemServiceTests
{
    public static TheoryData<string, string, string> OwnServiceCases => new()
    {
        // service, scenario, accept
        { "silent", "conflictHelper", "application/json" },
        { "silent", "conflictHelper", "text/plain" },
        { "silent", "exception", "application/json" },
        { "silent", "exception", "text/plain" },
        { "text", "conflictHelper", "application/json" },
        { "text", "conflictHelper", "text/plain" },
        { "text", "exception", "application/json" },
        { "text", "exception", "text/plain" },
    };

    // 1.9.0's answers: status, whole Content-Type header ("" for none), body.
    private static readonly Dictionary<(string Service, string Scenario), (HttpStatusCode Status, string ContentType, string Body)> Expected = new()
    {
        [("silent", "conflictHelper")] = (HttpStatusCode.Conflict, "", ""),
        [("silent", "exception")] = (HttpStatusCode.InternalServerError, "", ""),
        [("text", "conflictHelper")] = (HttpStatusCode.Conflict, "text/plain", "own service: 409 Conflict"),
        [("text", "exception")] = (HttpStatusCode.InternalServerError, "text/plain", "own service: 500 Internal server error"),
    };

    [Theory]
    [MemberData(nameof(OwnServiceCases))]
    public async Task Own_Service_Should_Answer_As_In_1_9_0(string service, string scenario, string accept)
    {
        // Arrange
        await using var app = await ProblemAcceptHeaderTests.StartAppAsync(scenario, before: services =>
        {
            if (service == "silent")
                services.AddSingleton<IProblemDetailsService, SilentProblemService>();
            else
                services.AddSingleton<IProblemDetailsService, TextProblemService>();
        });
        var client = app.GetTestClient();

        // Act
        var resp = await ProblemAcceptHeaderTests.SendAsync(client, scenario, accept);
        var body = await resp.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        var (status, contentType, expectedBody) = Expected[(service, scenario)];
        resp.StatusCode.Should().Be(status, "body: {0}", body);
        (resp.Content.Headers.ContentType?.ToString() ?? "").Should().Be(contentType);
        body.Should().Be(expectedBody);
    }

    [Theory]
    [InlineData("text/plain")]
    [InlineData("application/json")]
    public async Task Mocked_Service_Should_Get_One_WriteAsync_And_Nothing_Else_Be_Asked(string accept)
    {
        // Arrange
        var service = new CountingProblemService();
        var requestServices = new RecordingServiceProvider(service);
        var ctx = new DefaultHttpContext { RequestServices = requestServices };
        ctx.Request.Headers.Accept = accept;

        // Act
        await ctx.Conflict(detail: "Order 42 already exists").ExecuteAsync(ctx);

        // Assert
        service.WriteCalls.Should().Be(1);
        service.TryWriteCalls.Should().Be(0);
        requestServices.Requested.Should().Equal(typeof(IProblemDetailsService));
        ctx.Response.StatusCode.Should().Be(StatusCodes.Status409Conflict);
    }

    /// <summary>
    /// What the toolkit's recognition of the framework's service rests on. If a later version of
    /// ASP.NET Core changes either, this test fails, instead of the fix silently turning off.
    /// </summary>
    [Fact]
    public async Task Framework_Service_Should_Be_Registered_By_Type_And_Decline_Without_Writing()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddProblemDetails();
        var descriptor = services.Single(d => d.ServiceType == typeof(IProblemDetailsService));
        services.AddOptions();
        services.AddLogging();
        await using var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<IProblemDetailsService>();

        var ctx = new DefaultHttpContext { RequestServices = provider, Response = { Body = new MemoryStream() } };
        ctx.Request.Headers.Accept = "text/plain";
        var context = new ProblemDetailsContext
        {
            HttpContext = ctx,
            ProblemDetails = new ProblemDetails { Status = StatusCodes.Status409Conflict },
        };

        // Act
        var written = await service.TryWriteAsync(context);

        // Assert
        descriptor.ImplementationType.Should().NotBeNull("AddProblemDetails registers the service by its implementation type");
        service.GetType().Should().Be(descriptor.ImplementationType!);
        written.Should().BeFalse("no writer of the framework accepts text/plain");
        ctx.Response.Body.Length.Should().Be(0, "the service writes nothing when it declines");
    }

    private sealed class SilentProblemService : IProblemDetailsService
    {
        public ValueTask WriteAsync(ProblemDetailsContext context) => ValueTask.CompletedTask;
    }

    private sealed class TextProblemService : IProblemDetailsService
    {
        public ValueTask WriteAsync(ProblemDetailsContext context)
        {
            context.HttpContext.Response.ContentType = "text/plain";
            return new ValueTask(context.HttpContext.Response.WriteAsync(
                $"own service: {context.ProblemDetails.Status} {context.ProblemDetails.Title}"));
        }
    }

    private sealed class CountingProblemService : IProblemDetailsService
    {
        public int WriteCalls { get; private set; }

        public int TryWriteCalls { get; private set; }

        public ValueTask WriteAsync(ProblemDetailsContext context)
        {
            WriteCalls++;
            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> TryWriteAsync(ProblemDetailsContext context)
        {
            TryWriteCalls++;
            return ValueTask.FromResult(false);
        }
    }

    private sealed class RecordingServiceProvider(IProblemDetailsService service) : IServiceProvider
    {
        public List<Type> Requested { get; } = [];

        public object? GetService(Type serviceType)
        {
            Requested.Add(serviceType);
            return serviceType == typeof(IProblemDetailsService) ? service : null;
        }
    }
}

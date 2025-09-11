using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using AmasiaLabs.Toolkit.MinimalApi.Problems;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AmasiaLabs.Toolkit.MinimalApi.Tests.Integration;

public class ProblemDetailsIntegrationTests
{
    [Fact]
    public async Task Fallback_404_Should_Return_ProblemDetails()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddLogging();
        // Provide ProblemHandlingOptions needed by fallback/problem middlewares
        builder.Services.AddSingleton(new ProblemHandlingOptions());

        var app = builder.Build();
        // Do not enable exception handler in this test host
        app.MapGet("/ok", () => Results.Ok());
        app.MapProblemFallback404();

        await app.StartAsync(TestContext.Current.CancellationToken);
        var client = app.GetTestClient();

        var resp = 
            await client.GetAsync("/missing", TestContext.Current.CancellationToken);
        
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var pd =
            await resp.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken: TestContext.Current.CancellationToken);
        pd.Should().NotBeNull();
        pd.Status.Should().Be(StatusCodes.Status404NotFound);
        pd.Title.Should().Be("Not found");
    }

    [Fact]
    public async Task MethodNotAllowed_405_Should_Return_ProblemDetails()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddLogging();
        builder.Services.AddSingleton(new ProblemHandlingOptions());

        var app = builder.Build();
        // Do not enable exception handler in this test host
        app.MapGet("/only-get", () => Results.Ok());
        app.UseProblemMethodNotAllowed();

        await app.StartAsync(TestContext.Current.CancellationToken);
        var client = app.GetTestClient();

        var resp =
            await client.PostAsync(
                "/only-get", 
                content: null, cancellationToken: 
                TestContext.Current.CancellationToken);
        resp.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
        var pd = 
            await resp.Content.ReadFromJsonAsync<ProblemDetails>(
                cancellationToken: TestContext.Current.CancellationToken);
        pd.Should().NotBeNull();
        pd.Status.Should().Be(StatusCodes.Status405MethodNotAllowed);
        pd.Title.Should().Be("Method not allowed");
    }
}

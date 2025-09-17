using System.Net;
using System.Net.Http.Json;
using AmasiaLabs.Toolkit.MinimalApi.Problems;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AmasiaLabs.Toolkit.MinimalApi.Tests.Integration;

public class StatusProblemMiddlewareTests
{
    [Theory]
    [InlineData(400, "Bad request", nameof(ErrorHandlingExtensions))]
    [InlineData(409, "Conflict", nameof(ErrorHandlingExtensions))]
    [InlineData(422, "Unprocessable content", nameof(ErrorHandlingExtensions))]
    public async Task Status_Code_Should_Be_Converted_To_ProblemDetails(int code, string expectedTitle, string _)
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddLogging();
        builder.Services.AddGlobalExceptionHandling();

        var app = builder.Build();
        // Register problem middlewares
        app.UseProblemBadRequest();
        app.UseProblemConflict();
        app.UseProblemUnprocessableContent();

        app.MapGet("/t", (ctx) => { ctx.Response.StatusCode = code; return Task.CompletedTask; });

        await app.StartAsync(TestContext.Current.CancellationToken);
        var client = app.GetTestClient();

        // Act
        var resp = await client.GetAsync("/t", TestContext.Current.CancellationToken);
        var pd = await resp.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        resp.StatusCode.Should().Be((HttpStatusCode)code);
        pd!.Status.Should().Be(code);
        pd.Title.Should().Be(expectedTitle);
    }

    [Fact]
    public async Task TooManyRequests_Should_Be_Converted_To_ProblemDetails()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddLogging();
        builder.Services.AddGlobalExceptionHandling();

        var app = builder.Build();
        app.UseProblemTooManyRequests();
        app.MapGet("/t", (ctx) => { ctx.Response.StatusCode = StatusCodes.Status429TooManyRequests; return Task.CompletedTask; });

        await app.StartAsync(TestContext.Current.CancellationToken);
        var client = app.GetTestClient();

        // Act
        var resp = await client.GetAsync("/t", TestContext.Current.CancellationToken);
        var pd = await resp.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        resp.StatusCode.Should().Be((HttpStatusCode)429);
        pd!.Status.Should().Be(429);
        pd.Title.Should().Be("Too many requests");
    }
}

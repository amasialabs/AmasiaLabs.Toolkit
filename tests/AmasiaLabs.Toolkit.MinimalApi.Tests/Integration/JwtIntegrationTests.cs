using System.Net;
using System.Net.Http.Json;
using AmasiaLabs.Toolkit.MinimalApi.Auth.Jwt;
using AmasiaLabs.Toolkit.MinimalApi.Problems;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AmasiaLabs.Toolkit.MinimalApi.Tests.Integration;

public class JwtIntegrationTests
{
    [Fact]
    public async Task Missing_Jwt_Should_Return_401_ProblemDetails()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddLogging();
        builder.Services.AddGlobalExceptionHandling();

        builder.Services
            .AddAuthentication()
            .AddJwtBearerWithProblemDetails(
                issuer: "test-iss",
                audience: "test-aud",
                signingKey: "super-secret-test-key");
        builder.Services.AddAuthorization();

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapGet("/secure", () => Results.Ok())
           .RequireAuthorization()
           .ProducesDefaultProblems();

        await app.StartAsync(TestContext.Current.CancellationToken);
        var client = app.GetTestClient();

        // Act
        var resp = await client.GetAsync("/secure", TestContext.Current.CancellationToken);
        var pd = await resp.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        pd!.Status.Should().Be(StatusCodes.Status401Unauthorized);
        pd.Title.Should().Be("Unauthorized");
    }

    [Fact]
    public async Task Custom_OnMessageReceived_Should_Run_AND_Toolkit_Should_Still_Return_401_ProblemDetails()
    {
        // Arrange — user provides their own OnMessageReceived (e.g., custom token source).
        // Toolkit should still produce ProblemDetails 401 when no token is present.
        var customRan = false;
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddLogging();
        builder.Services.AddGlobalExceptionHandling();

        builder.Services
            .AddAuthentication()
            .AddJwtBearerWithProblemDetails(
                issuer: "test-iss",
                audience: "test-aud",
                signingKey: "super-secret-test-key",
                cookieName: "jc",
                configure: o =>
                {
                    var prev = o.Events.OnMessageReceived;
                    o.Events.OnMessageReceived = async ctx =>
                    {
                        customRan = true;
                        await prev(ctx);
                    };
                });
        builder.Services.AddAuthorization();

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapGet("/secure", () => Results.Ok()).RequireAuthorization().ProducesDefaultProblems();

        await app.StartAsync(TestContext.Current.CancellationToken);
        var client = app.GetTestClient();

        // Act
        var resp = await client.GetAsync("/secure", TestContext.Current.CancellationToken);
        var pd = await resp.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken: TestContext.Current.CancellationToken);

        // Assert — both ran
        customRan.Should().BeTrue("user-provided OnMessageReceived must execute");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        pd!.Status.Should().Be(StatusCodes.Status401Unauthorized);
        pd.Title.Should().Be("Unauthorized");
    }

    [Fact]
    public async Task Replacing_Events_Wholesale_Should_Not_Break_Toolkit_ProblemDetails_401()
    {
        // Arrange — user replaces options.Events with a fresh JwtBearerEvents (the natural
        // pattern when adding a single OnTokenValidated handler). With the old code, this
        // wholesale-replace would silently drop the toolkit's OnChallenge handler and the
        // 401 would fall back to ASP.NET defaults instead of ProblemDetails. With the new
        // composition approach, the toolkit's events compose ON TOP of the replacement.
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddLogging();
        builder.Services.AddGlobalExceptionHandling();

        builder.Services
            .AddAuthentication()
            .AddJwtBearerWithProblemDetails(
                issuer: "test-iss",
                audience: "test-aud",
                signingKey: "super-secret-test-key",
                cookieName: "jc",
                configure: o =>
                {
                    o.Events = new JwtBearerEvents
                    {
                        OnTokenValidated = _ => Task.CompletedTask,
                    };
                });
        builder.Services.AddAuthorization();

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapGet("/secure", () => Results.Ok()).RequireAuthorization().ProducesDefaultProblems();

        await app.StartAsync(TestContext.Current.CancellationToken);
        var client = app.GetTestClient();

        // Act
        var resp = await client.GetAsync("/secure", TestContext.Current.CancellationToken);
        var pd = await resp.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken: TestContext.Current.CancellationToken);

        // Assert — toolkit ProblemDetails still wins despite the wholesale replacement
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        pd!.Status.Should().Be(StatusCodes.Status401Unauthorized);
        pd.Title.Should().Be("Unauthorized");
    }

    [Fact]
    public async Task JwtAuthenticationOptions_Overload_Should_Return_401_ProblemDetails_For_Missing_Token()
    {
        // Arrange — exercise the new options-object overload to confirm parity with the
        // explicit-args overload's behavior.
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddLogging();
        builder.Services.AddGlobalExceptionHandling();

        builder.Services
            .AddAuthentication()
            .AddJwtBearerWithProblemDetails(new JwtAuthenticationOptions
            {
                Issuer = "test-iss",
                Audience = "test-aud",
                SigningKey = "super-secret-test-key",
            });
        builder.Services.AddAuthorization();

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapGet("/secure", () => Results.Ok()).RequireAuthorization().ProducesDefaultProblems();

        await app.StartAsync(TestContext.Current.CancellationToken);
        var client = app.GetTestClient();

        // Act
        var resp = await client.GetAsync("/secure", TestContext.Current.CancellationToken);
        var pd = await resp.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        pd!.Status.Should().Be(StatusCodes.Status401Unauthorized);
        pd.Title.Should().Be("Unauthorized");
    }
}

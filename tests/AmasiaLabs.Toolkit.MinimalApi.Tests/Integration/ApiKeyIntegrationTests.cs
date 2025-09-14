using System.Net;
using System.Net.Http.Json;
using AmasiaLabs.Toolkit.MinimalApi.Auth.ApiKey;
using AmasiaLabs.Toolkit.MinimalApi.Problems;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using Xunit;

namespace AmasiaLabs.Toolkit.MinimalApi.Tests.Integration;

public class ApiKeyIntegrationTests
{
    [Fact]
    public async Task Missing_ApiKey_Should_Return_401_ProblemDetails()
    {
        // Arrange
        var (_, client) = await BuildApp();

        // Act
        var resp = await client.GetAsync("/sec", TestContext.Current.CancellationToken);
        var pd = await resp.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        pd!.Status.Should().Be(StatusCodes.Status401Unauthorized);
        pd.Title.Should().Be("Unauthorized");
    }

    [Fact]
    public async Task Valid_ApiKey_Should_Return_200()
    {
        // Arrange
        var (_, client) = await BuildApp();
        var req = new HttpRequestMessage(HttpMethod.Get, "/sec");
        req.Headers.Add("X-Api-Key", TestApiKeyProvider.ValidKey);

        // Act
        var resp = await client.SendAsync(req, TestContext.Current.CancellationToken);

        // Assert
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Authenticated_But_Forbidden_Should_Return_403_ProblemDetails()
    {
        // Arrange
        var (app, client) = await BuildApp(configureAuth: opts =>
        {
            // Return a subject and a claim that is NOT admin
            opts.ClaimsFactory = subject =>
            [
                new Claim(ClaimTypes.NameIdentifier, subject),
                new Claim(ClaimTypes.Role, "user")
            ];
        }, configurePolicies: services =>
        {
            services.AddAuthorization(o =>
            {
                o.AddPolicy("AdminOnly", p => p.RequireRole("admin").RequireAuthenticatedUser());
            });
        });

        app.MapGet("/admin", () => Results.Ok()).RequireAuthorization("AdminOnly");

        // Act
        var req = new HttpRequestMessage(HttpMethod.Get, "/admin");
        req.Headers.Add("X-Api-Key", TestApiKeyProvider.ValidKey);
        var resp = await client.SendAsync(req, TestContext.Current.CancellationToken);
        var pd = await resp.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        pd!.Status.Should().Be(StatusCodes.Status403Forbidden);
        pd.Title.Should().Be("Forbidden");
    }

    private static async Task<(WebApplication App, HttpClient Client)> BuildApp(
        Action<ApiKeyAuthenticationOptions>? configureAuth = null,
        Action<IServiceCollection>? configurePolicies = null)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddLogging();
        builder.Services.AddSingleton(new ProblemHandlingOptions());

        builder.Services.AddSingleton<IApiKeyProvider, TestApiKeyProvider>();
        builder.Services.AddApiKeyAuthentication(setAsDefault: true, configure: configureAuth);
        builder.Services.AddAuthorization();
        configurePolicies?.Invoke(builder.Services);

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapGet("/sec", () => Results.Ok()).RequireAuthorization("ApiKeyOnly").ProducesDefaultProblems();

        await app.StartAsync(TestContext.Current.CancellationToken);
        return (app, app.GetTestClient());
    }

    private sealed class TestApiKeyProvider : IApiKeyProvider
    {
        public const string ValidKey = "k-api";
        public Task<string?> GetSubjectAsync(string apiKey, CancellationToken cancellationToken = default)
            => Task.FromResult(apiKey == ValidKey ? "api-client" : null);
    }
}

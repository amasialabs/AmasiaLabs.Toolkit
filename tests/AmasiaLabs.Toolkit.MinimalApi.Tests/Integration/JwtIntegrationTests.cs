using System.Net;
using System.Net.Http.Json;
using AmasiaLabs.Toolkit.MinimalApi.Auth.Jwt;
using AmasiaLabs.Toolkit.MinimalApi.Problems;
using FluentAssertions;
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
        builder.Services.AddSingleton(new ProblemHandlingOptions());

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
}

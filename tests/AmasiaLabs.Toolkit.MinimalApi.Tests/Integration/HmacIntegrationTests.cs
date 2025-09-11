using System.Net;
using System.Net.Http.Json;
using System.Text;
using AmasiaLabs.Toolkit.MinimalApi.Auth.Hmac;
using AmasiaLabs.Toolkit.MinimalApi.Problems;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AmasiaLabs.Toolkit.MinimalApi.Tests.Integration;

public class HmacIntegrationTests
{
    [Fact]
    public async Task Missing_Hmac_Should_Return_401_ProblemDetails()
    {
        var (_, client) = await BuildHmacApp();

        var resp = 
            await client.PostAsync(
                "/secure-echo", 
                new StringContent("hello", Encoding.UTF8, "text/plain"), 
                TestContext.Current.CancellationToken);
        
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        resp.Headers.WwwAuthenticate.ToString().Should().Contain("Hmac");
        var pd = 
            await resp.Content.ReadFromJsonAsync<ProblemDetails>(
                cancellationToken: TestContext.Current.CancellationToken);
        
        pd!.Status.Should().Be(StatusCodes.Status401Unauthorized);
        pd.Title.Should().Be("Unauthorized");
    }

    [Fact]
    public async Task Valid_Hmac_Should_Return_200_And_Signed_Response_Header()
    {
        var (_, client) = await BuildHmacApp(signResponses: true);
        var body = "hello";
        var key = TestHmacKeyProvider.Key;
        var signature = TestHmacSignature.Sign(key, body);

        var req = new HttpRequestMessage(HttpMethod.Post, "/secure-echo")
        {
            Content = new StringContent(body, Encoding.UTF8, "text/plain")
        };
        req.Headers.Add("X-Client-Id", TestHmacKeyProvider.ClientId);
        req.Headers.Add("X-Signature", signature);

        var resp = 
            await client.SendAsync(req, TestContext.Current.CancellationToken);
        
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        resp.Headers.TryGetValues("X-Signature", out var values).Should().BeTrue();
        // The response body is JSON for string content: "hello"
        var expectedResponsePayload = $"\"{body}\"";
        values!.Single().Should().Be(TestHmacSignature.Sign(key, expectedResponsePayload));
    }

    private static async Task<(WebApplication App, HttpClient Client)> BuildHmacApp(bool signResponses = false)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddLogging();
        // Provide ProblemHandlingOptions for HMAC challenge/forbid ProblemDetails
        builder.Services.AddSingleton(new ProblemHandlingOptions());
        // Avoid global exception handler to keep test host minimal

        // Test HMAC dependencies
        builder.Services.AddSingleton<IHmacKeyProvider, TestHmacKeyProvider>();
        builder.Services.AddSingleton<IHmacSignatureValidator, TestHmacSignatureValidator>();
        builder.Services.AddSingleton<IHmacSignatureSigner, TestHmacSignatureSigner>();

        builder.Services.AddHmacAuthentication(setAsDefault: true);
        builder.Services.AddAuthorization();

        var app = builder.Build();
        // Not enabling exception handler in tests; not needed for auth flows
        app.UseAuthentication();
        app.UseAuthorization();
        if (signResponses)
        {
            app.UseHmacResponseSigning();
        }

        app.MapPost("/secure-echo", async (HttpContext ctx) =>
        {
            using var reader = new StreamReader(ctx.Request.Body, Encoding.UTF8);
            var payload = await reader.ReadToEndAsync();
            return Results.Ok(payload);
        })
        .RequireAuthorization(HmacAuthenticationHandler.PolicyName)
        .ProducesDefaultProblems();

        await app.StartAsync();
        return (app, app.GetTestClient());
    }

    private static class TestHmacSignature
    {
        public static string Sign(string key, string payload) => $"sig:{key}:{payload}";
    }

    private sealed class TestHmacKeyProvider : IHmacKeyProvider
    {
        public const string ClientId = "c1";
        public const string Key = "k1";
        public Task<string?> GetKeyAsync(string clientId, CancellationToken cancellationToken = default)
            => Task.FromResult(clientId == ClientId ? Key : null);
    }

    private sealed class TestHmacSignatureValidator : IHmacSignatureValidator
    {
        public bool ValidateSignature(string key, string signature, string payload)
            => signature == TestHmacSignature.Sign(key, payload);
    }

    private sealed class TestHmacSignatureSigner : IHmacSignatureSigner
    {
        public string ComputeSignature(string key, string payload) => TestHmacSignature.Sign(key, payload);
    }
}

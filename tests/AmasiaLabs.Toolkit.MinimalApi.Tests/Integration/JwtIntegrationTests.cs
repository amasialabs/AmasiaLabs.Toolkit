using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using AmasiaLabs.Toolkit.MinimalApi.Auth.Jwt;
using AmasiaLabs.Toolkit.MinimalApi.Problems;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
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

    [Fact]
    public async Task User_OnChallenge_Calling_HandleResponse_Should_Suppress_Toolkit_ProblemDetails()
    {
        // The user's OnChallenge calls ctx.HandleResponse() to signal "I'm taking over"
        // and sets a custom status. The toolkit must respect Handled and not overwrite,
        // even though the response body has not yet started.
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
                    o.Events.OnChallenge = ctx =>
                    {
                        ctx.HandleResponse();
                        ctx.Response.StatusCode = StatusCodes.Status418ImATeapot;
                        return Task.CompletedTask;
                    };
                });
        builder.Services.AddAuthorization();

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapGet("/secure", () => Results.Ok()).RequireAuthorization();

        await app.StartAsync(TestContext.Current.CancellationToken);
        var client = app.GetTestClient();

        var resp = await client.GetAsync("/secure", TestContext.Current.CancellationToken);

        resp.StatusCode.Should().Be((HttpStatusCode)418, "user handled the challenge with HandleResponse(); toolkit must not overwrite");
    }

    [Fact]
    public async Task Custom_JwtBearerEvents_Subclass_Override_Should_Coexist_With_Toolkit()
    {
        // The user provides a subclass overriding the virtual MessageReceived method
        // (instead of assigning OnMessageReceived delegate). The toolkit's forwarding
        // wrapper must still call the override AND still emit ProblemDetails 401.
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
                    o.Events = new TrackingEvents(() => customRan = true);
                });
        builder.Services.AddAuthorization();

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapGet("/secure", () => Results.Ok()).RequireAuthorization().ProducesDefaultProblems();

        await app.StartAsync(TestContext.Current.CancellationToken);
        var client = app.GetTestClient();

        var resp = await client.GetAsync("/secure", TestContext.Current.CancellationToken);
        var pd = await resp.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken: TestContext.Current.CancellationToken);

        customRan.Should().BeTrue("subclass override of MessageReceived must be invoked");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        pd!.Status.Should().Be(StatusCodes.Status401Unauthorized);
        pd.Title.Should().Be("Unauthorized");
    }

    private sealed class TrackingEvents(Action onMessageReceived) : JwtBearerEvents
    {
        public override Task MessageReceived(MessageReceivedContext context)
        {
            onMessageReceived();
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task User_OnChallenge_HandleResponse_Without_Touching_Status_Should_Suppress_Toolkit_ProblemDetails_Body()
    {
        // Strict variant of the Handled-flag regression: user's OnChallenge calls
        // HandleResponse() and does NOTHING else — no status change, no body. The toolkit
        // must not write its ProblemDetails JSON body on top.
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
                    o.Events.OnChallenge = ctx =>
                    {
                        ctx.HandleResponse();
                        return Task.CompletedTask;
                    };
                });
        builder.Services.AddAuthorization();

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapGet("/secure", () => Results.Ok()).RequireAuthorization();

        await app.StartAsync(TestContext.Current.CancellationToken);
        var client = app.GetTestClient();

        var resp = await client.GetAsync("/secure", TestContext.Current.CancellationToken);
        var body = await resp.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        body.Should().BeNullOrEmpty("toolkit must not write ProblemDetails when user has called HandleResponse()");
    }

    [Fact]
    public async Task Subclass_Override_Of_MessageReceived_Should_Coexist_With_Toolkit_Cookie_Extraction()
    {
        // User subclasses JwtBearerEvents and overrides MessageReceived (without setting
        // ctx.Token). The toolkit's forwarding wrapper must still extract the cookie value
        // into ctx.Token, so JWT validation is attempted (which fails for an invalid token,
        // firing AuthenticationFailed — proof that the cookie was propagated).
        var customRan = false;
        var validationAttempted = false;

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
                    o.Events = new InstrumentedEvents(
                        onMessageReceived: () => customRan = true,
                        onAuthenticationFailed: () => validationAttempted = true);
                });
        builder.Services.AddAuthorization();

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapGet("/secure", () => Results.Ok()).RequireAuthorization().ProducesDefaultProblems();

        await app.StartAsync(TestContext.Current.CancellationToken);
        var client = app.GetTestClient();

        var req = new HttpRequestMessage(HttpMethod.Get, "/secure");
        req.Headers.Add("Cookie", "jc=not-a-real-jwt");
        var resp = await client.SendAsync(req, TestContext.Current.CancellationToken);

        customRan.Should().BeTrue("user subclass MessageReceived must execute");
        validationAttempted.Should().BeTrue("toolkit cookie extraction must propagate cookie value to ctx.Token, triggering JWT validation");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Subclass_Override_Of_Challenge_Without_Handled_Should_Still_Allow_Toolkit_ProblemDetails()
    {
        // User subclasses JwtBearerEvents and overrides Challenge but does NOT call HandleResponse()
        // and does NOT start the response. Toolkit must still emit 401 ProblemDetails.
        var challengeOverrideRan = false;

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
                    o.Events = new ChallengeTrackingEvents(() => challengeOverrideRan = true);
                });
        builder.Services.AddAuthorization();

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapGet("/secure", () => Results.Ok()).RequireAuthorization().ProducesDefaultProblems();

        await app.StartAsync(TestContext.Current.CancellationToken);
        var client = app.GetTestClient();

        var resp = await client.GetAsync("/secure", TestContext.Current.CancellationToken);
        var pd = await resp.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken: TestContext.Current.CancellationToken);

        challengeOverrideRan.Should().BeTrue("subclass Challenge override must execute");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        pd!.Title.Should().Be("Unauthorized");
    }

    [Fact]
    public async Task User_OnTokenValidated_Should_Run_Through_Forwarding_Wrapper()
    {
        // Hooking OnTokenValidated must still fire when a valid token is presented — proves
        // the wrapper forwards TokenValidated to the inner events instance. UseSlidingRefresh
        // (which works by chaining OnTokenValidated) relies on this same forwarding path.
        const string issuer = "test-iss";
        const string audience = "test-aud";
        const string signingKey = "super-secret-test-key-that-is-long-enough-for-hs256";

        var validatedRan = false;
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddLogging();
        builder.Services.AddGlobalExceptionHandling();

        builder.Services
            .AddAuthentication()
            .AddJwtBearerWithProblemDetails(
                issuer: issuer,
                audience: audience,
                signingKey: signingKey,
                cookieName: "jc",
                configure: o =>
                {
                    o.Events.OnTokenValidated = ctx =>
                    {
                        validatedRan = true;
                        return Task.CompletedTask;
                    };
                });
        builder.Services.AddAuthorization();

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapGet("/secure", () => Results.Ok()).RequireAuthorization();

        await app.StartAsync(TestContext.Current.CancellationToken);
        var client = app.GetTestClient();

        var token = CreateValidTestJwt(issuer, audience, signingKey);
        var req = new HttpRequestMessage(HttpMethod.Get, "/secure");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resp = await client.SendAsync(req, TestContext.Current.CancellationToken);

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        validatedRan.Should().BeTrue("user OnTokenValidated must run through the forwarding wrapper");
    }

    [Fact]
    public async Task Calling_AddJwtBearerWithProblemDetails_Twice_Should_Not_Recursively_Wrap_Events()
    {
        // Defensive guard: if the JwtBearerOptions configure callback runs more than once
        // (e.g. configuration callbacks chained), ComposeToolkitEvents must not stack
        // ToolkitJwtBearerEvents on top of itself. First-call-wins; subsequent calls are
        // no-ops for events composition. Behavior should still produce 401 ProblemDetails.
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddLogging();
        builder.Services.AddGlobalExceptionHandling();

        var authBuilder = builder.Services.AddAuthentication();
        authBuilder.AddJwtBearerWithProblemDetails(
            issuer: "test-iss",
            audience: "test-aud",
            signingKey: "super-secret-test-key");
        // Apply a second configure callback against the same scheme; this re-enters the
        // ComposeToolkitEvents path on the same JwtBearerOptions instance.
        builder.Services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, _ => { });
        builder.Services.AddAuthorization();

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapGet("/secure", () => Results.Ok()).RequireAuthorization().ProducesDefaultProblems();

        await app.StartAsync(TestContext.Current.CancellationToken);
        var client = app.GetTestClient();

        var resp = await client.GetAsync("/secure", TestContext.Current.CancellationToken);
        var pd = await resp.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken: TestContext.Current.CancellationToken);

        // Single-layer wrap → exactly one ProblemDetails body, status 401.
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        pd!.Status.Should().Be(StatusCodes.Status401Unauthorized);
        pd.Title.Should().Be("Unauthorized");
    }

    private static string CreateValidTestJwt(string issuer, string audience, string signingKey)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            Expires = DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = creds,
            Subject = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "test-user")]),
        };
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    private sealed class InstrumentedEvents(Action onMessageReceived, Action onAuthenticationFailed) : JwtBearerEvents
    {
        public override Task MessageReceived(MessageReceivedContext context)
        {
            onMessageReceived();
            return Task.CompletedTask;
        }

        public override Task AuthenticationFailed(AuthenticationFailedContext context)
        {
            onAuthenticationFailed();
            return Task.CompletedTask;
        }
    }

    private sealed class ChallengeTrackingEvents(Action onChallenge) : JwtBearerEvents
    {
        public override Task Challenge(JwtBearerChallengeContext context)
        {
            onChallenge();
            return Task.CompletedTask;
        }
    }
}

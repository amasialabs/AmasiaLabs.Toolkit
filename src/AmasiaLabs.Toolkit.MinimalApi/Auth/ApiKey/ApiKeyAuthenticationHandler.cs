using System.Security.Claims;
using System.Text.Encodings.Web;
using AmasiaLabs.Toolkit.MinimalApi.Problems;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace AmasiaLabs.Toolkit.MinimalApi.Auth.ApiKey;

/// <summary>
/// API Key authentication handler with ProblemDetails for 401/403.
/// </summary>
public sealed class ApiKeyAuthenticationHandler(
    IApiKeyProvider provider,
    IOptionsMonitor<ApiKeyAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<ApiKeyAuthenticationOptions>(options, logger, encoder)
{
    public const string SchemeName = "ApiKey";

    // ReSharper disable once CognitiveComplexity
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var endpoint = Context.GetEndpoint();
        var isAnonymous = endpoint?.Metadata.GetMetadata<IAllowAnonymous>() is not null;
        var requiresAuth = endpoint?.Metadata.GetOrderedMetadata<IAuthorizeData>().Any() == true;

        if (isAnonymous || !requiresAuth)
            return AuthenticateResult.NoResult();

        string? apiKey = null;
        if (Options.Location is ApiKeyLocation.Header or ApiKeyLocation.HeaderOrQuery)
        {
            if (Request.Headers.TryGetValue(Options.HeaderName, out var headerValues) && !StringValues.IsNullOrEmpty(headerValues))
            {
                apiKey = headerValues.ToString();
            }
        }

        if (apiKey is null && Options.Location is ApiKeyLocation.Query or ApiKeyLocation.HeaderOrQuery)
        {
            if (Request.Query.TryGetValue(Options.QueryParameterName, out var queryValues) && !StringValues.IsNullOrEmpty(queryValues))
            {
                apiKey = queryValues.ToString();
            }
        }

        if (string.IsNullOrWhiteSpace(apiKey))
            return AuthenticateResult.NoResult();

        try
        {
            var subject = await provider.GetSubjectAsync(apiKey, Context.RequestAborted);
            if (string.IsNullOrWhiteSpace(subject))
                return AuthenticateResult.Fail("Invalid API key");

            var claims = Options.ClaimsFactory?.Invoke(subject)
                         ?? new[] { new Claim(ClaimTypes.NameIdentifier, subject) };

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return AuthenticateResult.Success(ticket);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error validating API key");
            return AuthenticateResult.Fail("API key validation error");
        }
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.Headers.Append("WWW-Authenticate", Options.WwwAuthenticateScheme);

        var opts = Context.RequestServices.GetRequiredService<ProblemHandlingOptions>();
        var status = StatusCodes.Status401Unauthorized;
        var pd = new ProblemDetails
        {
            Status = status,
            Title = "Unauthorized",
            Detail = opts.GetMessage(status),
            Instance = Context.Request.Path,
            Type = opts.TypeUriFactory(status),
            Extensions = { ["traceId"] = Context.TraceIdentifier }
        };

        Response.ContentType = "application/problem+json";
        return Response.WriteAsJsonAsync(pd);
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;

        var opts = Context.RequestServices.GetRequiredService<ProblemHandlingOptions>();
        var status = StatusCodes.Status403Forbidden;
        var pd = new ProblemDetails
        {
            Status = status,
            Title = "Forbidden",
            Detail = opts.GetMessage(status),
            Instance = Context.Request.Path,
            Type = opts.TypeUriFactory(status),
            Extensions = { ["traceId"] = Context.TraceIdentifier }
        };

        Response.ContentType = "application/problem+json";
        return Response.WriteAsJsonAsync(pd);
    }
}

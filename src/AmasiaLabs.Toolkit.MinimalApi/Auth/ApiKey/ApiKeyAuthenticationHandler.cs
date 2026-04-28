using System.Security.Claims;
using System.Text.Encodings.Web;
using AmasiaLabs.Toolkit.MinimalApi.Problems;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace AmasiaLabs.Toolkit.MinimalApi.Auth.ApiKey;

/// <summary>
/// API Key authentication handler with ProblemDetails for 401/403.
/// </summary>
public sealed partial class ApiKeyAuthenticationHandler(
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
                         ?? [new Claim(ClaimTypes.NameIdentifier, subject)];

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return AuthenticateResult.Success(ticket);
        }
        catch (Exception ex)
        {
            LogApiKeyValidationError(Logger, ex);
            return AuthenticateResult.Fail("API key validation error");
        }
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Headers.Append("WWW-Authenticate", Options.WwwAuthenticateScheme);
        return ProblemDetailsWriter.WriteAsync(Context, StatusCodes.Status401Unauthorized, "Unauthorized");
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
        => ProblemDetailsWriter.WriteAsync(Context, StatusCodes.Status403Forbidden, "Forbidden");

    [LoggerMessage(Level = LogLevel.Error, Message = "Error validating API key")]
    private static partial void LogApiKeyValidationError(ILogger logger, Exception exception);
}

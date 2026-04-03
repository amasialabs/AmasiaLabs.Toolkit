using System.Security.Claims;
using System.Text;
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

namespace AmasiaLabs.Toolkit.MinimalApi.Auth.Hmac;

/// <summary>
/// Minimal, opinionated HMAC authentication handler.
/// - Expects headers: X-Client-Id and X-Signature
/// - Validates signature via <see cref="IHmacSignatureValidator"/> using the full request body as payload.
/// - Emits RFC 7807 ProblemDetails for 401/403 using <see cref="ProblemHandlingOptions"/>.
/// </summary>
public sealed partial class HmacAuthenticationHandler(
    IHmacSignatureValidator signatureValidator,
    IHmacKeyProvider keyProvider,
    IOptionsMonitor<HmacAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<HmacAuthenticationOptions>(options, logger, encoder)
{
    public const string SchemeName = "Hmac";
    public const string PolicyName = "HmacOnly";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var endpoint = Context.GetEndpoint();
        var isAnonymous = endpoint?.Metadata.GetMetadata<IAllowAnonymous>() is not null;
        var requiresAuth = endpoint?.Metadata.GetOrderedMetadata<IAuthorizeData>().Any() == true;

        var clientIdHeader = Options.ClientIdHeader;
        var signatureHeader = Options.SignatureHeader;

        if (isAnonymous || !requiresAuth
                       || !Request.Headers.TryGetValue(clientIdHeader, out StringValues clientIdValues)
                       || StringValues.IsNullOrEmpty(clientIdValues)
                       || !Request.Headers.TryGetValue(signatureHeader, out StringValues signatureValues)
                       || StringValues.IsNullOrEmpty(signatureValues))
        {
            return AuthenticateResult.NoResult();
        }

        var clientId = clientIdValues.ToString();
        var signature = signatureValues.ToString();

        string payload;
        if (Options.BuildPayload is not null)
        {
            payload = await Options.BuildPayload(Context);
        }
        else
        {
            var body = string.Empty;
            if (Request.ContentLength.GetValueOrDefault() > 0)
            {
                Request.EnableBuffering();
                using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
                body = await reader.ReadToEndAsync();
                Request.Body.Position = 0;
            }
            payload = body;
        }

        try
        {
            var clientKey = await keyProvider.GetKeyAsync(clientId, Context.RequestAborted);
            if (clientKey is null || !signatureValidator.ValidateSignature(clientKey, signature, payload))
            {
                return AuthenticateResult.Fail("Invalid signature");
            }
        }
        catch (Exception ex)
        {
            LogSignatureValidationError(Logger, ex, clientId);
            return AuthenticateResult.Fail("Signature validation error");
        }

        var claims = Options.ClaimsFactory?.Invoke(clientId)
                     ?? [new Claim(ClaimTypes.NameIdentifier, clientId), new Claim("client_id", clientId)];

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.Headers.Append("WWW-Authenticate", Options.WwwAuthenticateScheme);

        var status = StatusCodes.Status401Unauthorized;
        var pd = new ProblemDetails { Status = status, Title = "Unauthorized" };

        var pds = Context.RequestServices.GetRequiredService<IProblemDetailsService>();
        var context = new ProblemDetailsContext
        {
            HttpContext = Context,
            ProblemDetails = pd
        };
        return pds.WriteAsync(context).AsTask();
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;

        var status = StatusCodes.Status403Forbidden;
        var pd = new ProblemDetails { Status = status, Title = "Forbidden" };

        var pds = Context.RequestServices.GetRequiredService<IProblemDetailsService>();
        var context = new ProblemDetailsContext
        {
            HttpContext = Context,
            ProblemDetails = pd
        };
        return pds.WriteAsync(context).AsTask();
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Error validating signature for client {ClientId}")]
    private static partial void LogSignatureValidationError(ILogger logger, Exception exception, string clientId);
}

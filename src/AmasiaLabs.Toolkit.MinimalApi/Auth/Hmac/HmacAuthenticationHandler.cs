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
public sealed class HmacAuthenticationHandler(
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
            Logger.LogError(ex, "Error validating signature for client {ClientId}", clientId);
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

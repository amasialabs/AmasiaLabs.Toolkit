using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using AmasiaLabs.Toolkit.MinimalApi.Problems;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
            // Custom hook always wins — preserves backward compatibility.
            payload = await Options.BuildPayload(Context);
        }
        else if (Options.PayloadMode == HmacPayloadMode.CanonicalRequest)
        {
            var canonicalResult = await TryBuildCanonicalRequestAsync(clientId, Context.RequestAborted);
            if (!canonicalResult.Success)
                return AuthenticateResult.Fail(canonicalResult.Reason!);

            payload = canonicalResult.Payload!;
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
        Response.Headers.Append("WWW-Authenticate", Options.WwwAuthenticateScheme);
        return ProblemDetailsWriter.WriteAsync(Context, StatusCodes.Status401Unauthorized, "Unauthorized");
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
        => ProblemDetailsWriter.WriteAsync(Context, StatusCodes.Status403Forbidden, "Forbidden");

    private async Task<CanonicalPayloadResult> TryBuildCanonicalRequestAsync(string clientId, CancellationToken cancellationToken)
    {
        if (!Request.Headers.TryGetValue(Options.TimestampHeader, out var timestampValues)
            || StringValues.IsNullOrEmpty(timestampValues))
        {
            return CanonicalPayloadResult.Fail($"Missing {Options.TimestampHeader} header");
        }

        if (!Request.Headers.TryGetValue(Options.NonceHeader, out var nonceValues)
            || StringValues.IsNullOrEmpty(nonceValues))
        {
            return CanonicalPayloadResult.Fail($"Missing {Options.NonceHeader} header");
        }

        var timestampRaw = timestampValues.ToString();
        if (!DateTimeOffset.TryParse(timestampRaw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var timestamp))
        {
            return CanonicalPayloadResult.Fail("Invalid timestamp format");
        }

        var now = DateTimeOffset.UtcNow;
        var skew = (now - timestamp).Duration();
        if (skew > Options.AllowedClockSkew)
        {
            return CanonicalPayloadResult.Fail("Timestamp outside allowed clock skew");
        }

        var nonce = nonceValues.ToString();

        var nonceStore = Context.RequestServices.GetService<IHmacNonceStore>();
        if (nonceStore is not null)
        {
            var registered = await nonceStore.TryRegisterAsync(clientId, nonce, timestamp, cancellationToken);
            if (!registered)
            {
                return CanonicalPayloadResult.Fail("Nonce already used");
            }
        }

        var bodyHashHex = await ComputeBodyHashHexAsync(cancellationToken);

        var canonical = string.Join('\n',
            Request.Method.ToUpperInvariant(),
            Request.Path.HasValue ? Request.Path.Value : "/",
            Request.QueryString.HasValue ? Request.QueryString.Value!.TrimStart('?') : string.Empty,
            timestamp.ToString("O", CultureInfo.InvariantCulture),
            nonce,
            bodyHashHex);

        return CanonicalPayloadResult.Ok(canonical);
    }

    private async Task<string> ComputeBodyHashHexAsync(CancellationToken cancellationToken)
    {
        if (Request.ContentLength.GetValueOrDefault() <= 0)
        {
            return Convert.ToHexString(SHA256.HashData(ReadOnlySpan<byte>.Empty)).ToLowerInvariant();
        }

        Request.EnableBuffering();
        using var sha = SHA256.Create();
        var hash = await sha.ComputeHashAsync(Request.Body, cancellationToken);
        Request.Body.Position = 0;
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private readonly struct CanonicalPayloadResult
    {
        public bool Success { get; }
        public string? Payload { get; }
        public string? Reason { get; }

        private CanonicalPayloadResult(bool success, string? payload, string? reason)
        {
            Success = success;
            Payload = payload;
            Reason = reason;
        }

        public static CanonicalPayloadResult Ok(string payload) => new(true, payload, null);
        public static CanonicalPayloadResult Fail(string reason) => new(false, null, reason);
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Error validating signature for client {ClientId}")]
    private static partial void LogSignatureValidationError(ILogger logger, Exception exception, string clientId);
}

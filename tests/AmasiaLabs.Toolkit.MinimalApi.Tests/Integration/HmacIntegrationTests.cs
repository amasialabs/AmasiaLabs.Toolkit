using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
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
        // Arrange
        var (_, client) = await BuildHmacApp();

        // Act
        var resp = await client.PostAsync(
            "/secure-echo",
            new StringContent("hello", Encoding.UTF8, "text/plain"),
            TestContext.Current.CancellationToken);
        var pd = await resp.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        resp.Headers.WwwAuthenticate.ToString().Should().Contain("Hmac");
        pd!.Status.Should().Be(StatusCodes.Status401Unauthorized);
        pd.Title.Should().Be("Unauthorized");
    }

    [Fact]
    public async Task Valid_Hmac_Should_Return_200_And_Signed_Response_Header()
    {
        // Arrange
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

        // Act
        var resp = await client.SendAsync(req, TestContext.Current.CancellationToken);

        // Assert
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        resp.Headers.TryGetValues("X-Signature", out var values).Should().BeTrue();
        // The response body is JSON for string content: "hello"
        var expectedResponsePayload = $"\"{body}\"";
        values!.Single().Should().Be(TestHmacSignature.Sign(key, expectedResponsePayload));
    }

    private static async Task<(WebApplication App, HttpClient Client)> BuildHmacApp(
        bool signResponses = false,
        Action<HmacAuthenticationOptions>? configureHmac = null,
        Action<IServiceCollection>? extraServices = null)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddLogging();
        builder.Services.AddGlobalExceptionHandling();
        // Avoid global exception handler to keep the test host minimal

        // Test HMAC dependencies
        builder.Services.AddSingleton<IHmacKeyProvider, TestHmacKeyProvider>();
        builder.Services.AddSingleton<IHmacSignatureValidator, TestHmacSignatureValidator>();
        builder.Services.AddSingleton<IHmacSignatureSigner, TestHmacSignatureSigner>();
        extraServices?.Invoke(builder.Services);

        builder.Services.AddHmacAuthentication(setAsDefault: true, configure: configureHmac);
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
        // Header-safe digest. Real HMAC also produces fixed-size encoded output,
        // so this is a closer stand-in than a literal payload echo. Without this
        // hashing step a canonical-request payload (which contains "\n") would
        // produce a signature string that HttpClient rejects in headers.
        public static string Sign(string key, string payload)
        {
            var bytes = Encoding.UTF8.GetBytes($"sig:{key}:{payload}");
            return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        }
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

    // =====================================================================
    // Canonical-request mode (PR-5). The BodyOnly default is preserved by
    // the tests above; everything below here exercises the opt-in mode.
    // =====================================================================

    [Fact]
    public async Task Canonical_Mode_Valid_Request_Should_Return_200()
    {
        var (_, client) = await BuildHmacApp(configureHmac: o => o.PayloadMode = HmacPayloadMode.CanonicalRequest);

        var body = "hello";
        var timestamp = DateTimeOffset.UtcNow;
        var nonce = "n-1";
        var canonical = BuildCanonicalRequest("POST", "/secure-echo", "", timestamp.ToString("O", CultureInfo.InvariantCulture), nonce, body);
        var signature = TestHmacSignature.Sign(TestHmacKeyProvider.Key, canonical);

        var req = new HttpRequestMessage(HttpMethod.Post, "/secure-echo")
        {
            Content = new StringContent(body, Encoding.UTF8, "text/plain"),
        };
        req.Headers.Add("X-Client-Id", TestHmacKeyProvider.ClientId);
        req.Headers.Add("X-Signature", signature);
        req.Headers.Add("X-Timestamp", timestamp.ToString("O", CultureInfo.InvariantCulture));
        req.Headers.Add("X-Nonce", nonce);

        var resp = await client.SendAsync(req, TestContext.Current.CancellationToken);

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Canonical_Mode_Missing_Timestamp_Should_Return_401()
    {
        var (_, client) = await BuildHmacApp(configureHmac: o => o.PayloadMode = HmacPayloadMode.CanonicalRequest);

        var req = new HttpRequestMessage(HttpMethod.Post, "/secure-echo")
        {
            Content = new StringContent("hello", Encoding.UTF8, "text/plain"),
        };
        req.Headers.Add("X-Client-Id", TestHmacKeyProvider.ClientId);
        req.Headers.Add("X-Signature", "anything");
        req.Headers.Add("X-Nonce", "n-1");
        // No X-Timestamp

        var resp = await client.SendAsync(req, TestContext.Current.CancellationToken);
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Canonical_Mode_Missing_Nonce_Should_Return_401()
    {
        var (_, client) = await BuildHmacApp(configureHmac: o => o.PayloadMode = HmacPayloadMode.CanonicalRequest);

        var req = new HttpRequestMessage(HttpMethod.Post, "/secure-echo")
        {
            Content = new StringContent("hello", Encoding.UTF8, "text/plain"),
        };
        req.Headers.Add("X-Client-Id", TestHmacKeyProvider.ClientId);
        req.Headers.Add("X-Signature", "anything");
        req.Headers.Add("X-Timestamp", DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture));
        // No X-Nonce

        var resp = await client.SendAsync(req, TestContext.Current.CancellationToken);
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Canonical_Mode_Stale_Timestamp_Should_Return_401()
    {
        // 10 minutes back is outside the default 5-minute clock-skew window.
        var (_, client) = await BuildHmacApp(configureHmac: o => o.PayloadMode = HmacPayloadMode.CanonicalRequest);

        var body = "hello";
        var staleTimestamp = DateTimeOffset.UtcNow.AddMinutes(-10);
        var nonce = "n-stale";
        var canonical = BuildCanonicalRequest("POST", "/secure-echo", "", staleTimestamp.ToString("O", CultureInfo.InvariantCulture), nonce, body);
        var signature = TestHmacSignature.Sign(TestHmacKeyProvider.Key, canonical);

        var req = new HttpRequestMessage(HttpMethod.Post, "/secure-echo")
        {
            Content = new StringContent(body, Encoding.UTF8, "text/plain"),
        };
        req.Headers.Add("X-Client-Id", TestHmacKeyProvider.ClientId);
        req.Headers.Add("X-Signature", signature);
        req.Headers.Add("X-Timestamp", staleTimestamp.ToString("O", CultureInfo.InvariantCulture));
        req.Headers.Add("X-Nonce", nonce);

        var resp = await client.SendAsync(req, TestContext.Current.CancellationToken);
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Canonical_Mode_With_NonceStore_Should_Reject_Replay()
    {
        var nonceStore = new InMemoryNonceStore();
        var (_, client) = await BuildHmacApp(
            configureHmac: o => o.PayloadMode = HmacPayloadMode.CanonicalRequest,
            extraServices: s => s.AddSingleton<IHmacNonceStore>(nonceStore));

        var body = "hello";
        var timestamp = DateTimeOffset.UtcNow;
        var nonce = "n-replay";
        var canonical = BuildCanonicalRequest("POST", "/secure-echo", "", timestamp.ToString("O", CultureInfo.InvariantCulture), nonce, body);
        var signature = TestHmacSignature.Sign(TestHmacKeyProvider.Key, canonical);

        HttpRequestMessage MakeReq()
        {
            var r = new HttpRequestMessage(HttpMethod.Post, "/secure-echo")
            {
                Content = new StringContent(body, Encoding.UTF8, "text/plain"),
            };
            r.Headers.Add("X-Client-Id", TestHmacKeyProvider.ClientId);
            r.Headers.Add("X-Signature", signature);
            r.Headers.Add("X-Timestamp", timestamp.ToString("O", CultureInfo.InvariantCulture));
            r.Headers.Add("X-Nonce", nonce);
            return r;
        }

        var first = await client.SendAsync(MakeReq(), TestContext.Current.CancellationToken);
        first.StatusCode.Should().Be(HttpStatusCode.OK, "first request with the nonce should succeed");

        var second = await client.SendAsync(MakeReq(), TestContext.Current.CancellationToken);
        second.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "replay of the same nonce must be rejected");
    }

    [Fact]
    public async Task BodyOnly_Default_Mode_Should_Not_Require_Timestamp_Or_Nonce()
    {
        // Regression guard: default (BodyOnly) mode keeps ignoring timestamp/nonce.
        // Mirrors the existing Valid_Hmac test but explicitly omits the new headers
        // and asserts no leakage of canonical-mode requirements into defaults.
        var (_, client) = await BuildHmacApp();

        var body = "hello";
        var signature = TestHmacSignature.Sign(TestHmacKeyProvider.Key, body);
        var req = new HttpRequestMessage(HttpMethod.Post, "/secure-echo")
        {
            Content = new StringContent(body, Encoding.UTF8, "text/plain"),
        };
        req.Headers.Add("X-Client-Id", TestHmacKeyProvider.ClientId);
        req.Headers.Add("X-Signature", signature);
        // No X-Timestamp, no X-Nonce

        var resp = await client.SendAsync(req, TestContext.Current.CancellationToken);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Canonical_Mode_Chunked_Body_Should_Be_Authenticated()
    {
        // Regression for the body-hash bug: when a request uses chunked transfer encoding,
        // ContentLength is null on the server side. Old code treated null as "empty body"
        // and signed SHA256(empty), which (a) rejects legitimate chunked clients that signed
        // the real body hash, and (b) lets attackers slip arbitrary body bytes past auth as
        // long as they sign with the empty-body hash. The fix is to always read the stream.
        var (_, client) = await BuildHmacApp(configureHmac: o => o.PayloadMode = HmacPayloadMode.CanonicalRequest);

        var body = "important payload that must be authenticated";
        var bodyBytes = Encoding.UTF8.GetBytes(body);
        var timestamp = DateTimeOffset.UtcNow;
        var nonce = "n-chunked";
        var canonical = BuildCanonicalRequest("POST", "/secure-echo", "", timestamp.ToString("O", CultureInfo.InvariantCulture), nonce, body);
        var signature = TestHmacSignature.Sign(TestHmacKeyProvider.Key, canonical);

        var req = new HttpRequestMessage(HttpMethod.Post, "/secure-echo")
        {
            Content = new ChunkedTestContent(bodyBytes, "text/plain"),
        };
        req.Headers.Add("X-Client-Id", TestHmacKeyProvider.ClientId);
        req.Headers.Add("X-Signature", signature);
        req.Headers.Add("X-Timestamp", timestamp.ToString("O", CultureInfo.InvariantCulture));
        req.Headers.Add("X-Nonce", nonce);

        var resp = await client.SendAsync(req, TestContext.Current.CancellationToken);

        resp.StatusCode.Should().Be(HttpStatusCode.OK, "chunked-body request signed against the real body hash must validate");
    }

    [Fact]
    public async Task Canonical_Mode_Invalid_Signature_Should_Not_Consume_Nonce()
    {
        // Regression for the nonce-poisoning bug: a request with an invalid signature
        // must not consume the (clientId, nonce) tuple in the nonce store. Otherwise
        // an attacker (or a buggy client) can lock out the legitimate request.
        var nonceStore = new InMemoryNonceStore();
        var (_, client) = await BuildHmacApp(
            configureHmac: o => o.PayloadMode = HmacPayloadMode.CanonicalRequest,
            extraServices: s => s.AddSingleton<IHmacNonceStore>(nonceStore));

        var body = "hello";
        var timestamp = DateTimeOffset.UtcNow;
        var nonce = "n-poisoning-attempt";

        // First request: valid timestamp + nonce, INVALID signature
        var bad = new HttpRequestMessage(HttpMethod.Post, "/secure-echo")
        {
            Content = new StringContent(body, Encoding.UTF8, "text/plain"),
        };
        bad.Headers.Add("X-Client-Id", TestHmacKeyProvider.ClientId);
        bad.Headers.Add("X-Signature", "0badbadbadbadbad");
        bad.Headers.Add("X-Timestamp", timestamp.ToString("O", CultureInfo.InvariantCulture));
        bad.Headers.Add("X-Nonce", nonce);
        var badResp = await client.SendAsync(bad, TestContext.Current.CancellationToken);
        badResp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Second request: SAME nonce, VALID signature — must succeed because the
        // bogus first request must NOT have consumed the nonce.
        var canonical = BuildCanonicalRequest("POST", "/secure-echo", "", timestamp.ToString("O", CultureInfo.InvariantCulture), nonce, body);
        var goodSignature = TestHmacSignature.Sign(TestHmacKeyProvider.Key, canonical);
        var good = new HttpRequestMessage(HttpMethod.Post, "/secure-echo")
        {
            Content = new StringContent(body, Encoding.UTF8, "text/plain"),
        };
        good.Headers.Add("X-Client-Id", TestHmacKeyProvider.ClientId);
        good.Headers.Add("X-Signature", goodSignature);
        good.Headers.Add("X-Timestamp", timestamp.ToString("O", CultureInfo.InvariantCulture));
        good.Headers.Add("X-Nonce", nonce);
        var goodResp = await client.SendAsync(good, TestContext.Current.CancellationToken);
        goodResp.StatusCode.Should().Be(HttpStatusCode.OK, "legitimate request must succeed; bogus first request must not have poisoned the nonce");
    }

    [Fact]
    public async Task Canonical_Mode_Should_Sign_Raw_Timestamp_Header_Value()
    {
        // The canonical string signs exactly the timestamp string the client sent
        // in the header — not a server-side normalized form. This lets clients use
        // any DateTimeOffset-parseable representation (e.g. "2026-04-28T14:00:00Z")
        // without having to mirror the server's format choice.
        var (_, client) = await BuildHmacApp(configureHmac: o => o.PayloadMode = HmacPayloadMode.CanonicalRequest);

        // Pick a format that DateTimeOffset.Parse accepts but DateTimeOffset.ToString("O") would NOT produce
        // (no fractional seconds, "Z" suffix instead of "+00:00").
        var rawTimestamp = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
        var body = "hello";
        var nonce = "n-raw-ts";
        var canonical = BuildCanonicalRequest("POST", "/secure-echo", "", rawTimestamp, nonce, body);
        var signature = TestHmacSignature.Sign(TestHmacKeyProvider.Key, canonical);

        var req = new HttpRequestMessage(HttpMethod.Post, "/secure-echo")
        {
            Content = new StringContent(body, Encoding.UTF8, "text/plain"),
        };
        req.Headers.Add("X-Client-Id", TestHmacKeyProvider.ClientId);
        req.Headers.Add("X-Signature", signature);
        req.Headers.Add("X-Timestamp", rawTimestamp);
        req.Headers.Add("X-Nonce", nonce);

        var resp = await client.SendAsync(req, TestContext.Current.CancellationToken);
        resp.StatusCode.Should().Be(HttpStatusCode.OK, "server must sign the raw timestamp header value, not a server-side reformatted version");
    }

    [Fact]
    public async Task BodyOnly_Mode_Chunked_Body_Should_Be_Authenticated()
    {
        // Positive case: a legitimate client that streams the body with chunked transfer
        // encoding (ContentLength == null on the server) and signs the real body must
        // authenticate in the default BodyOnly mode. The old ContentLength>0 gate would
        // have signed the empty payload instead and rejected this valid request.
        var (_, client) = await BuildHmacApp();

        var body = "important payload that must be authenticated";
        var bodyBytes = Encoding.UTF8.GetBytes(body);
        var signature = TestHmacSignature.Sign(TestHmacKeyProvider.Key, body);

        var req = new HttpRequestMessage(HttpMethod.Post, "/secure-echo")
        {
            Content = new ChunkedTestContent(bodyBytes, "text/plain"),
        };
        req.Headers.Add("X-Client-Id", TestHmacKeyProvider.ClientId);
        req.Headers.Add("X-Signature", signature);

        var resp = await client.SendAsync(req, TestContext.Current.CancellationToken);

        resp.StatusCode.Should().Be(HttpStatusCode.OK, "a chunked body signed against its real content must validate in BodyOnly mode");
    }

    [Fact]
    public async Task BodyOnly_Mode_Empty_Signed_Chunked_Body_Should_Be_Rejected()
    {
        // The attack this fix closes: an attacker captures a signature computed over the
        // EMPTY payload (trivially available from any signed empty-body request), then
        // sends a chunked request (ContentLength == null) carrying an arbitrary body.
        // The old code treated the null length as an empty payload, matched the empty-body
        // signature, and let the injected body reach the endpoint authenticated. Reading
        // the stream unconditionally makes the signature no longer match.
        var (_, client) = await BuildHmacApp();

        var emptySignature = TestHmacSignature.Sign(TestHmacKeyProvider.Key, string.Empty);
        var maliciousBody = Encoding.UTF8.GetBytes("injected body the attacker never signed");

        var req = new HttpRequestMessage(HttpMethod.Post, "/secure-echo")
        {
            Content = new ChunkedTestContent(maliciousBody, "text/plain"),
        };
        req.Headers.Add("X-Client-Id", TestHmacKeyProvider.ClientId);
        req.Headers.Add("X-Signature", emptySignature);

        var resp = await client.SendAsync(req, TestContext.Current.CancellationToken);

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "a signature computed over the empty payload must not authenticate a non-empty chunked body");
    }

    [Fact]
    public async Task BodyOnly_Mode_Signed_Payload_Should_Commit_To_Raw_Body_Bytes_Including_Bom()
    {
        // Regression for the StreamReader BOM bug: the reader must not strip or act on a byte-order
        // mark, or the signed string stops committing to the exact bytes on the wire. A body whose
        // bytes begin with a UTF-8 BOM must authenticate when the client signs the byte-faithful
        // string (U+FEFF + text). The old reader stripped the BOM and signed "hello", which both
        // rejected this legitimate request and let a captured "hello" signature cover BOM-prefixed bytes.
        var (_, client) = await BuildHmacApp();

        byte[] rawBytes = [0xEF, 0xBB, 0xBF, .. Encoding.UTF8.GetBytes("hello")];
        const string signedString = "\uFEFFhello"; // exactly what the raw bytes decode to, byte-for-byte
        var signature = TestHmacSignature.Sign(TestHmacKeyProvider.Key, signedString);

        var content = new ByteArrayContent(rawBytes);
        content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        var req = new HttpRequestMessage(HttpMethod.Post, "/secure-echo") { Content = content };
        req.Headers.Add("X-Client-Id", TestHmacKeyProvider.ClientId);
        req.Headers.Add("X-Signature", signature);

        var resp = await client.SendAsync(req, TestContext.Current.CancellationToken);

        resp.StatusCode.Should().Be(HttpStatusCode.OK, "the signed payload must commit to the exact body bytes, including a leading BOM");
    }

    private static string BuildCanonicalRequest(
        string method, string path, string query, string rawTimestamp, string nonce, string body)
    {
        var bodyBytes = Encoding.UTF8.GetBytes(body);
        var bodyHashHex = Convert.ToHexString(SHA256.HashData(bodyBytes)).ToLowerInvariant();
        return string.Join('\n',
            method.ToUpperInvariant(),
            path,
            query,
            rawTimestamp,
            nonce,
            bodyHashHex);
    }

    private sealed class ChunkedTestContent : HttpContent
    {
        private readonly byte[] data;

        public ChunkedTestContent(byte[] data, string contentType)
        {
            this.data = data;
            Headers.ContentType = new MediaTypeHeaderValue(contentType);
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
            => stream.WriteAsync(data, 0, data.Length);

        // Returning false forces HttpClient to omit Content-Length and use chunked transfer
        // encoding; the server then sees Request.ContentLength == null.
        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }

    private sealed class InMemoryNonceStore : IHmacNonceStore
    {
        private readonly HashSet<string> seen = [];

        public Task<bool> TryRegisterAsync(string clientId, string nonce, DateTimeOffset timestamp, CancellationToken cancellationToken = default)
        {
            lock (seen)
            {
                return Task.FromResult(seen.Add($"{clientId}|{nonce}"));
            }
        }
    }
}

# AmasiaLabs.Toolkit.MinimalApi

ASP.NET Core Minimal API extensions for structured endpoints, global ProblemDetails handling, and auth helpers (JWT/HMAC/API Key), including optional HMAC response signing.

## Install

```bash
dotnet add package AmasiaLabs.Toolkit.MinimalApi
```

## Structured Endpoints

Implement `IEndpoints` in your endpoint types and register them once.

```csharp
public class UserEndpoints : IEndpoints
{
    public static void DefineEndPoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/users", GetUsers);
        app.MapPost("/users", CreateUser);
    }

    public static void AddEndPointServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IUserService, UserService>();
    }
}

// Program.cs
builder.Services.AddEndpoints<Program>(builder.Configuration);
app.UseEndpoints<Program>();

// Or simply
builder.Services.AddEndpoints();
app.UseEndpoints();
```

## Global ProblemDetails

Register once and return RFC 7807 across the app.

```csharp
// Program.cs
builder.Services.AddGlobalExceptionHandling(opts =>
{
    // Customize defaults if needed
    // opts.StatusMessages[404] = "User or resource not found";

    // Map specific exceptions
    opts.ExceptionMaps[typeof(MyDomainException)] = (ex, ctx) =>
        (StatusCodes.Status400BadRequest, "Validation failed", null);

    // Optional: include exception messages and log unhandled exceptions
    // opts.IncludeExceptionDetails = true; // expose ex.Message in ProblemDetails (use carefully)
    // opts.LogExceptions = true;          // log via ILogger in the global handler (default: true)
});

var app = builder.Build();
app.UseGlobalExceptionHandling();
```

### Error Handling Model

- Global exceptions: `GlobalExceptionHandler` handles thrown exceptions and maps them via `ProblemHandlingOptions` into RFC 7807 using `IProblemDetailsService`.
- Status codes without exceptions: middlewares convert bare HTTP statuses to ProblemDetails so responses stay consistent:
  - 404 fallback: `app.MapProblemFallback404()`
  - 405 method not allowed: `app.UseProblemMethodNotAllowed()`
  - 400/409/422 defaults: `app.UseProblemBadRequest()`, `app.UseProblemConflict()`, `app.UseProblemUnprocessableContent()`
  - 429 throttling: `app.UseProblemTooManyRequests()` (and see rate limiter sample below)
- Centralized formatting: `AddGlobalExceptionHandling` registers `AddProblemDetails` with defaults (type/instance/traceId and default messages). Any `ProblemDetails` written through `IProblemDetailsService` gets these applied automatically.

Annotate routes for OpenAPI:

```csharp
app.MapGet("/users", () => Results.Ok())
   .ProducesDefaultProblems();
```

Note: `AddGlobalExceptionHandling` registers ASP.NET Core's `IProblemDetailsService`. If you use problem middlewares or the 404 fallback without it, register `builder.Services.AddProblemDetails()` yourself.

Fallback + 405:

```csharp
app.MapProblemFallback404();
app.UseProblemMethodNotAllowed();
```

Throttling (429 Too Many Requests):

```csharp
app.UseProblemTooManyRequests();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = static (context, token) =>
    {
        var httpContext = context.HttpContext;
        var opts = httpContext.RequestServices.GetRequiredService<ProblemHandlingOptions>();
        var pds = httpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
        var status = StatusCodes.Status429TooManyRequests;
        var pd = new ProblemDetails
        {
            Status = status,
            Title = "Too many requests",
            Detail = opts.GetMessage(status),
            Instance = httpContext.Request.Path,
            Type = opts.TypeUriFactory(status)
        };

        httpContext.Response.StatusCode = status;
        var ctx = new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = pd
        };
        return pds.WriteAsync(ctx);
    };
});
```

### Problem result helpers

```csharp
app.MapPost("/orders", async (HttpContext ctx, CreateOrder dto, IValidator<CreateOrder> validator) =>
{
    var validation = await validator.ValidateAsync(dto);
    if (!validation.IsValid)
    {
        var errors = validation.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

        return ctx.Unprocessable(
            detail: "Validation failed",
            extensions: new Dictionary<string, object?> { ["errors"] = errors }
        );
    }

    if (await EmailExists(dto.Email))
        return ctx.Conflict(detail: "Email already exists");

    return Results.Created($"/orders/{42}", new { id = 42 });
});

app.MapPost("/parse-json", (HttpContext ctx, string body) =>
{
    if (!IsValidJson(body))
        return ctx.BadRequest(detail: "Invalid JSON format");

    return Results.Ok();
});

// Optional middleware for defaults
app.UseProblemConflict();
app.UseProblemUnprocessableContent();
app.UseProblemBadRequest();
```

For authentication-related statuses on routes that are not behind authorization middleware (like `/login`):
- Use `ctx.Unauthorized()` and `ctx.Forbidden()` to emit RFC 7807 ProblemDetails for 401/403.

## JWT Auth Defaults

Ready-made helpers for JWT Bearer + cookie token extraction + 401/403 ProblemDetails.

```csharp
// Registers authentication with sensible defaults
// Default config path: "Amasia:Toolkit:MinimalApi:Jwt" (Issuer, Audience, Key).
builder.Services.AddJwtAuthentication(builder.Configuration);

// Or if you already have an AuthenticationBuilder:
builder.Services
    .AddAuthentication()
    .AddJwtBearerWithProblemDetails(builder.Configuration);

// Or explicit values (and optional cookie name):
builder.Services
    .AddAuthentication()
    .AddJwtBearerWithProblemDetails(
        issuer: builder.Configuration["Amasia:Toolkit:MinimalApi:Jwt:Issuer"]!,
        audience: builder.Configuration["Amasia:Toolkit:MinimalApi:Jwt:Audience"]!,
        signingKey: builder.Configuration["Amasia:Toolkit:MinimalApi:Jwt:Key"]!,
        cookieName: "jc");
```

Expected configuration keys:
- `Amasia:Toolkit:MinimalApi:Jwt:Issuer`
- `Amasia:Toolkit:MinimalApi:Jwt:Audience`
- `Amasia:Toolkit:MinimalApi:Jwt:Key`

### Sliding refresh (auto-renew token)

You can enable sliding refresh so that, when a valid JWT is near expiry, the server issues a fresh token and sets it in a cookie. Provide your own claims refresh provider that re-hydrates claims from your domain (e.g., DB), and enable the hook on the JWT handler.

```csharp
using AmasiaLabs.Toolkit.MinimalApi.Auth.Jwt;

// 1) Register sliding refresh services and your provider
builder.Services.AddJwtSlidingRefresh<MyClaimsRefreshProvider>(
    configureTokenFactory: o =>
    {
        o.TokenLifetime = TimeSpan.FromHours(1);
        o.CookieName = "jc"; // should match the cookie used for extraction
        o.CookieSameSite = SameSiteMode.Strict;
        o.CookieSecure = true;
    },
    configureSliding: o => o.RefreshThreshold = TimeSpan.FromMinutes(5));

// 2) Enable sliding refresh on JWT bearer
builder.Services
    .AddAuthentication()
    .AddJwtBearerWithProblemDetails(builder.Configuration, cookieName: "jc", configure: options =>
    {
        options.UseSlidingRefresh();
    });

// Implement your provider in the app
public sealed class MyClaimsRefreshProvider : IJwtClaimsRefreshProvider
{
    public async Task<IEnumerable<Claim>?> RefreshClaimsAsync(HttpContext context, ClaimsPrincipal principal, CancellationToken ct)
    {
        // Example: load user by email claim and build fresh claims
        var email = principal.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrWhiteSpace(email)) return null;

        var userRepo = context.RequestServices.GetRequiredService<IMyUserRepository>();
        var user = await userRepo.GetByEmailAsync(email, ct);
        if (user is null) return null; // fall back to existing claims

        return new[]
        {
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
            // ... other claims/roles as needed
        };
    }
}
```

With this in place, protected endpoints only need `RequireAuthorization()`; token renewal happens centrally on `OnTokenValidated` without per-endpoint code.

### Login flow (username/password)

Use a domain-specific authenticator to validate credentials and issue a cookie with a JWT using the toolkit’s token factory and cookie writer:

```csharp
using AmasiaLabs.Toolkit.MinimalApi.Auth.Jwt;

app.MapPost("/login", async (HttpContext ctx, LoginDto dto,
    IUsernamePasswordAuthenticator auth,
    IJwtTokenFactory tokens,
    IJwtCookieWriter cookies,
    IOptionsMonitor<JwtBearerOptions> jwtOptionsMonitor) =>
{
    var claims = await auth.AuthenticateAsync(ctx, dto.Username, dto.Password, ctx.RequestAborted);
    if (claims is null)
        return ctx.Unauthorized(); // emits RFC 7807 ProblemDetails (401)

    // Use the JwtBearer options for the default scheme
    var jwtOptions = jwtOptionsMonitor.Get(JwtBearerDefaults.AuthenticationScheme);

    var token = tokens.CreateToken(claims, jwtOptions);
    cookies.WriteTokenCookie(ctx, token);
    return Results.NoContent();
});
```

## HMAC Auth

Lightweight HMAC authentication handler with RFC 7807 for 401/403 and an `HmacOnly` policy.

```csharp
// Implement and register providers in your app
builder.Services.AddSingleton<IHmacKeyProvider, MyHmacKeyProvider>();
builder.Services.AddSingleton<IHmacSignatureValidator, MyHmacSignatureValidator>();

builder.Services.AddHmacAuthentication(setAsDefault: false);

app.MapPost("/webhook", () => Results.Ok())
   .RequireAuthorization(HmacAuthenticationHandler.PolicyName)
   .ProducesDefaultProblems();

// Or 
builder.Services.AddHmacAuthentication();

app.MapPost("/webhook", () => Results.Ok())
   .RequireAuthorization()
   .ProducesDefaultProblems();
```

Advanced customization

```csharp
builder.Services.AddHmacAuthentication(configure: opts =>
{
    opts.ClientIdHeader = "X-My-Client";
    opts.SignatureHeader = "X-My-Signature"
    opts.WwwAuthenticateScheme = "HMAC-SHA256";

    // Custom payload (default: full body)
    opts.BuildPayload = async ctx =>
    {
        var method = ctx.Request.Method;
        var path = ctx.Request.Path.ToString();

        string body = string.Empty;
        if (ctx.Request.ContentLength.GetValueOrDefault() > 0)
        {
            ctx.Request.EnableBuffering();
            using var reader = new StreamReader(ctx.Request.Body, Encoding.UTF8, leaveOpen: true);
            body = await reader.ReadToEndAsync();
            ctx.Request.Body.Position = 0;
        }
        return $"{method}\n{path}\n{body}";
    };

    // Custom claims from client id
    opts.ClaimsFactory = clientId => new[]
    {
        new Claim(ClaimTypes.NameIdentifier, clientId),
        new Claim("client_id", clientId),
        new Claim(ClaimTypes.Role, "hmac-client")
    };
});
```

HMAC response signing

```csharp
builder.Services.AddSingleton<IHmacSignatureSigner, MyHmacSigner>();

app.UseHmacResponseSigning(options =>
{
    options.HeaderName = "X-Signature"; // header to append
    options.SuccessOnly = true;          // sign only 2xx responses
    // Optional: options.BuildPayload = (ctx, body) => Task.FromResult($"{ctx.Response.StatusCode}\n{body}");
});
```

By default, responses are signed only when the authenticated identity was issued by the `Hmac` scheme.

Default implementations

```csharp
using AmasiaLabs.Toolkit.MinimalApi.Auth.Hmac;
using AmasiaLabs.Toolkit.MinimalApi.Auth.Hmac.Defaults;

// Generic signature behavior
builder.Services.Configure<SignatureOptions>(o =>
{
    o.CheckSignature = true; // set false to bypass on dev
    o.Trim = true;           // remove CR/LF/TAB before signing
});

// Hex (lowercase) HMAC-SHA256 signer/validator
builder.Services.AddSingleton<IHmacSignatureSigner, HmacSha256HexSigner>();
builder.Services.AddSingleton<IHmacSignatureValidator, HmacSha256HexValidator>();

// Or Base64 HMAC-SHA256 variant
// builder.Services.AddSingleton<IHmacSignatureSigner, HmacSha256Base64Signer>();
// builder.Services.AddSingleton<IHmacSignatureValidator, HmacSha256Base64Validator>();
```

### Payload modes (BodyOnly default vs canonical request)

The handler supports two payload modes via `HmacAuthenticationOptions.PayloadMode`. The default preserves backward compatibility with existing clients.

```csharp
// Default (BodyOnly): the signature covers the raw request body only.
// No timestamp/nonce headers are required; existing clients keep working unchanged.
builder.Services.AddHmacAuthentication();

// Opt-in canonical-request mode: the signature covers
//   METHOD\nPATH\nQUERY\nTIMESTAMP\nNONCE\nSHA256_HEX(BODY)
// and the handler additionally enforces a clock-skew window and an optional nonce store.
builder.Services.AddHmacAuthentication(configure: opts =>
{
    opts.PayloadMode = HmacPayloadMode.CanonicalRequest;
    // opts.TimestampHeader = "X-Timestamp"; // default
    // opts.NonceHeader     = "X-Nonce";     // default
    // opts.AllowedClockSkew = TimeSpan.FromMinutes(5); // default
});

// Replay protection (optional): register an IHmacNonceStore implementation.
// If absent, the request is still validated, but nonces are not enforced single-use.
builder.Services.AddSingleton<IHmacNonceStore, MyRedisNonceStore>();
```

When `PayloadMode == CanonicalRequest`:
- Requests missing the timestamp or nonce header are rejected (401).
- Timestamps outside `AllowedClockSkew` (forward or backward) are rejected.
- If `IHmacNonceStore` is registered, repeated `(clientId, nonce)` tuples are rejected.

`BuildPayload` (when set) takes precedence over `PayloadMode` — callers who customized payload extraction keep their existing behavior regardless of mode.

## API Key Auth

API Key authentication with ProblemDetails for 401/403 and an `ApiKeyOnly` policy.

```csharp
// Implement provider in your app
public sealed class MyApiKeyProvider : IApiKeyProvider
{
    public Task<string?> GetSubjectAsync(string apiKey, CancellationToken ct = default)
        => Task.FromResult(apiKey == "my-secret" ? "api-client" : null);
}

builder.Services.AddSingleton<IApiKeyProvider, MyApiKeyProvider>();

builder.Services.AddApiKeyAuthentication(setAsDefault: false);

app.MapGet("/service-status", () => Results.Ok())
   .RequireAuthorization("ApiKeyOnly")
   .ProducesDefaultProblems();
```

Customize

```csharp
builder.Services.AddApiKeyAuthentication(configure: opts =>
{
    opts.Location = ApiKeyLocation.HeaderOrQuery; // Header, Query, or both
    opts.HeaderName = "X-Api-Key";
    opts.QueryParameterName = "api_key";
    opts.WwwAuthenticateScheme = "API-Key";

    opts.ClaimsFactory = subject => new[]
    {
        new Claim(ClaimTypes.NameIdentifier, subject),
        new Claim("auth_kind", "api-key")
    };
});
```

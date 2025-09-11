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
});

var app = builder.Build();
app.UseGlobalExceptionHandling();
```

Annotate routes for OpenAPI:

```csharp
app.MapGet("/users", () => Results.Ok())
   .ProducesDefaultProblems();
```

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
        httpContext.Response.ContentType = "application/problem+json";
        return httpContext.Response.WriteAsJsonAsync(pd, cancellationToken: token);
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

## JWT Auth Defaults

Ready-made helpers for JWT Bearer + cookie token extraction + 401/403 ProblemDetails.

```csharp
// Registers authentication with sensible defaults
builder.Services.AddJwtAuthentication(builder.Configuration);

// Or if you already have an AuthenticationBuilder:
builder.Services
    .AddAuthentication()
    .AddJwtBearerWithProblemDetails(builder.Configuration);

// Or explicit values (and optional cookie name):
builder.Services
    .AddAuthentication()
    .AddJwtBearerWithProblemDetails(
        issuer: builder.Configuration["Jwt:Issuer"]!,
        audience: builder.Configuration["Jwt:Audience"]!,
        signingKey: builder.Configuration["Jwt:Key"]!,
        cookieName: "jc");
```

Expected configuration keys:
- `Jwt:Issuer` and `Jwt:Audience`
- `Jwt:Key`

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

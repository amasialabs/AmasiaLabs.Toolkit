# AmasiaLabs.Toolkit

.NET toolkit library containing reusable components for modern application development.

## Projects

- **AmasiaLabs.Toolkit.MinimalApi** - ASP.NET Core Minimal API extensions for structured endpoint organization
- **AmasiaLabs.Toolkit.Podman** - Configuration extensions for Podman secrets management

## Installation

Packages are available via GitHub Packages:

```bash
dotnet add package AmasiaLabs.Toolkit.MinimalApi
dotnet add package AmasiaLabs.Toolkit.Podman
```

## Build

```bash
# Restore dependencies
dotnet restore

# Build all projects
dotnet build

# Build in Release mode
dotnet build --configuration Release

# Pack NuGet packages
dotnet pack --configuration Release --output ./artifacts
```

## Usage

### MinimalApi - Structured Endpoints

Define endpoints implementing `IEndpoints`:

```csharp
public class UserEndpoints : IEndpoints
{
    public static void DefineEndPoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/users", GetUsers);
        app.MapPost("/users", CreateUser);
    }
    // Optional: has a default no-op
    public static void AddEndPointServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IUserService, UserService>();
    }
}
```

Register in Program.cs:

```csharp
builder.Services.AddEndpoints<Program>(builder.Configuration);
app.UseEndpoints<Program>();
```

### MinimalApi - Global Exception Handling (ProblemDetails)

Add a global handler with RFC 7807 responses and customizable messages per status code and per exception type:

```csharp
// Program.cs
builder.Services.AddGlobalExceptionHandling(opts =>
{
    // Customize default messages per status code
    opts.StatusMessages[400] = "Request payload is invalid.";
    opts.StatusMessages[404] = "User or resource not found.";

    // Map specific exceptions to status/title
    opts.ExceptionMaps[typeof(MyDomainException)] = (ex, ctx) =>
        (StatusCodes.Status400BadRequest, "Validation failed", null);
});

var app = builder.Build();
app.UseGlobalExceptionHandling();
```

Annotate endpoints with standard problem responses for OpenAPI:

```csharp
app.MapGet("/users", () => Results.Ok())
   .ProducesDefaultProblems();
```

Fallback 404 as ProblemDetails and 405 handling:

```csharp
// Fallback for unmatched routes -> 404 ProblemDetails
app.MapProblemFallback404();

// Turn 405 Method Not Allowed into ProblemDetails (keeps Allow header)
app.UseProblemMethodNotAllowed();
```

Throttling (429 Too Many Requests):

```csharp
// Emit RFC 7807 for 429 responses (e.g., from rate limiting)
app.UseProblemTooManyRequests();

// Optional: configure ASP.NET Core Rate Limiter to return 429 and ProblemDetails
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

Authentication events (example with JWT) producing ProblemDetails for 401/403:

```csharp
builder.Services.AddAuthentication().AddJwtBearer(o =>
{
    o.Events = new JwtBearerEvents
    {
        OnChallenge = ctx =>
        {
            ctx.HandleResponse();
            var opts = ctx.HttpContext.RequestServices.GetRequiredService<ProblemHandlingOptions>();
            var pd = new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Unauthorized",
                Detail = opts.GetMessage(StatusCodes.Status401Unauthorized),
                Instance = ctx.HttpContext.Request.Path,
                Type = opts.TypeUriFactory(StatusCodes.Status401Unauthorized)
            };
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            ctx.Response.ContentType = "application/problem+json";
            return ctx.Response.WriteAsJsonAsync(pd);
        },
        OnForbidden = ctx =>
        {
            var opts = ctx.HttpContext.RequestServices.GetRequiredService<ProblemHandlingOptions>();
            var pd = new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Forbidden",
                Detail = opts.GetMessage(StatusCodes.Status403Forbidden),
                Instance = ctx.HttpContext.Request.Path,
                Type = opts.TypeUriFactory(StatusCodes.Status403Forbidden)
            };
            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            ctx.Response.ContentType = "application/problem+json";
            return ctx.Response.WriteAsJsonAsync(pd);
        }
    };
});
```

### Podman Secrets Configuration

Add Podman secrets to configuration:

```csharp
var builder = Host.CreateApplicationBuilder(args);

// Using default settings
builder.AddPodmanSecrets();

// Or with custom settings
builder.AddPodmanSecrets(
    directory: "/run/secrets",
    requireDirectory: false,
    throwOnError: false
);
```

#### Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `directory` | `/run/secrets` | Directory where Podman/Docker secret files are mounted |
| `requireDirectory` | `false` | Whether to throw if the secrets directory doesn't exist |
| `throwOnError` | `false` (for HostApplicationBuilder)<br>`true` (for IConfigurationBuilder) | Whether to throw exceptions when reading secret files fails |

Secret files are automatically mapped to configuration keys:
- File names with `__` are converted to `:` hierarchy (e.g., `database__password` → `database:password`)
- Hidden files (starting with `.`) are ignored
- Trailing newlines are automatically trimmed

## CI/CD

GitHub Actions automatically builds and publishes NuGet packages to GitHub Packages on push to main branch.

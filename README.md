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
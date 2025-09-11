# AmasiaLabs.Toolkit.Podman

Configuration extensions for reading Podman/Docker secrets into .NET configuration.

## Install

```bash
dotnet add package AmasiaLabs.Toolkit.Podman
```

## Usage

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

### Parameters

| Parameter          | Default                                                                    | Description                                                 |
|--------------------|----------------------------------------------------------------------------|-------------------------------------------------------------|
| `directory`        | `/run/secrets`                                                             | Directory where Podman/Docker secret files are mounted      |
| `requireDirectory` | `false`                                                                    | Whether to throw if the secrets directory doesn't exist     |
| `throwOnError`     | `false` (for HostApplicationBuilder)<br>`true` (for IConfigurationBuilder) | Whether to throw exceptions when reading secret files fails |

Secrets are mapped to configuration keys:
- File names with `__` become `:` hierarchy (e.g., `database__password` → `database:password`).
- Hidden files (starting with `.`) are ignored.
- Trailing newlines are trimmed.

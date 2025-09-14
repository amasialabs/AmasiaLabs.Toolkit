# AmasiaLabs.Toolkit

.NET toolkit library containing reusable components for modern application development.

## Projects

- AmasiaLabs.Toolkit.MinimalApi — ASP.NET Core Minimal API extensions. See `src/AmasiaLabs.Toolkit.MinimalApi/README.md`.
- AmasiaLabs.Toolkit.Podman — Podman/Docker secrets configuration helpers. See `src/AmasiaLabs.Toolkit.Podman/README.md`.
- AmasiaLabs.Toolkit.FlowflakeId — Snowflake-like ID generator with configurable epoch/time source. See `src/AmasiaLabs.Toolkit.FlowflakeId/README.md`.
- AmasiaLabs.Toolkit.FlowflakeId.Grpc — gRPC service wrapper around the Flowflake generator. See `src/AmasiaLabs.Toolkit.FlowflakeId.Grpc/README.md`.
- AmasiaLabs.Toolkit.FlowflakeId.Grpc.Client — gRPC client SDK that implements `IFlowflakeId`, with failover and ServerInfo caching. See `src/AmasiaLabs.Toolkit.FlowflakeId.Grpc.Client/README.md`.

## Installation

Packages are available via GitHub Packages:

```bash
dotnet add package AmasiaLabs.Toolkit.MinimalApi
dotnet add package AmasiaLabs.Toolkit.Podman
dotnet add package AmasiaLabs.Toolkit.FlowflakeId
dotnet add package AmasiaLabs.Toolkit.FlowflakeId.Grpc
dotnet add package AmasiaLabs.Toolkit.FlowflakeId.Grpc.Client
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

## CI/CD

GitHub Actions builds and publishes NuGet packages to GitHub Packages on pushes to the main branch.

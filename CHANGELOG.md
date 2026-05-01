# Changelog

All notable changes to this project will be documented in this file.

The format is based on Keep a Changelog (https://keepachangelog.com/en/1.1.0/),
and this project adheres to Semantic Versioning (https://semver.org/spec/v2.0.0.html).

## 2026-05-01 — gRPC packages removed

### Removed (BREAKING for gRPC consumers)
- `AmasiaLabs.Toolkit.FlowflakeId.Grpc` — gRPC service contracts and host wiring.
- `AmasiaLabs.Toolkit.FlowflakeId.Grpc.Client` — gRPC client SDK implementing `IFlowflakeId`.
- `AmasiaLabs.Toolkit.FlowflakeId.Grpc.Host` — gRPC server executable + container image.

These three packages stop receiving updates and will no longer be published. Consumers
should migrate to in-process `AmasiaLabs.Toolkit.FlowflakeId` (with the local generator
covering the vast majority of use cases) or build a thin wrapper for the few scenarios
that genuinely needed a remote ID server. With GUIDv7 widely available now, most callers
will not need either.

### Changed
- `AmasiaLabs.Toolkit.FlowflakeId` 1.4.15 → 1.4.16 (no API change; bump marks the
  ecosystem-level change of removed companion packages).
- `AmasiaLabs.Toolkit.FlowflakeId.Abstractions` 1.4.15 → 1.4.16 (same).
- `AmasiaLabs.Toolkit.FlowflakeId.Extensions` 1.4.16 → 1.4.17 (same).

### Removed (workflow)
- `.github/workflows/docker-publish.yml` — the only image it built was the gRPC host.
- gRPC-specific package versions and orphaned OpenTelemetry/ServiceDiscovery/Resilience
  entries from `Directory.Packages.props`.
- gRPC benchmarks from `tests/.../FlowflakeIdBenchmarks.cs`; the local FlowflakeId
  benchmarks remain.

The full pre-removal source is preserved on the `archive/with-grpc` branch.

## [1.7.0] - 2025-09-19

Semantic Versioning inference: minor (new features present; no explicit breaking changes detected). If uncertain, default would be patch.

### Added
- gRPC: add hosting helpers and improve test infrastructure ([f3ea553](https://github.com/amasialabs/AmasiaLabs.Toolkit/commit/f3ea5537894fd9654bbe3d8ee217b755fe6e6b19)).
- DI: add simplified overloads and improve documentation ([7d935d8](https://github.com/amasialabs/AmasiaLabs.Toolkit/commit/7d935d8fc379f3c6c67b5a710d24552251c59144)).
- FlowflakeId: support decode-only scenarios (refactor to enable decode-only use cases) ([cf00fa5](https://github.com/amasialabs/AmasiaLabs.Toolkit/commit/cf00fa5370e6215c9f2510d47fc5235088d8ce3d)).

### Changed
- MinimalApi: centralize ProblemDetails via IProblemDetailsService and CustomizeProblemDetails; switch middlewares/auth to service; adjust async signatures (ValueTask -> Task); update docs/tests ([6c0fabd](https://github.com/amasialabs/AmasiaLabs.Toolkit/commit/6c0fabdbef0a97532884de6065ea3f5d25323b6e)).

### Fixed
- MinimalApi: update JWT configuration path to MinimalApi namespace ([4cbaed8](https://github.com/amasialabs/AmasiaLabs.Toolkit/commit/4cbaed8ab4f3b3c6c0829ad641ffec06e74b1ec8)).

### Removed
- MinimalApi: delete unused CustomizedProblem ([6c0fabd](https://github.com/amasialabs/AmasiaLabs.Toolkit/commit/6c0fabdbef0a97532884de6065ea3f5d25323b6e)).


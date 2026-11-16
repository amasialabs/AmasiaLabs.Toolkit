# Changelog

All notable changes to this project will be documented in this file.

The format is based on Keep a Changelog (https://keepachangelog.com/en/1.1.0/),
and this project adheres to Semantic Versioning (https://semver.org/spec/v2.0.0.html).

## 2026-11-16 — problems answered as JSON whatever the request accepts

### Fixed
- `AmasiaLabs.Toolkit.MinimalApi`: the toolkit gave no problem document to a request whose
  `Accept` header admits no JSON (`text/plain`, `application/xml`, `text/html` alone). The
  default writer of ASP.NET Core declines such a request, and `IProblemDetailsService.WriteAsync`
  then throws. As a result:
  - an exception that reached `UseGlobalExceptionHandling` gave an empty 500, or the developer
    exception page in Development;
  - the result helpers (`ctx.Conflict(...)` and the others), the status middlewares
    (`UseProblemBadRequest`, `UseProblemConflict`, `UseProblemUnprocessableContent`,
    `UseProblemMethodNotAllowed`, `UseProblemTooManyRequests`), `MapProblemFallback404`, and the
    401 and 403 of the API-key, HMAC and JWT handlers threw.
- With the framework's `IProblemDetailsService`, which `AddGlobalExceptionHandling` and
  `AddProblemDetails()` register, the toolkit now answers its own problems as
  `application/problem+json`:
  - with `AddGlobalExceptionHandling`, all of the above, in the toolkit's form;
  - with `AddProblemDetails()` alone, all of the above but the exception handler, with the
    framework's defaults and the application's own `CustomizeProblemDetails`, run once. This
    set-up has no exception handler of the toolkit: an exception that nothing catches is
    answered as before, by the server's empty 500 in Production and by the developer exception
    page in Development.
- With an application's own `IProblemDetailsService`, the toolkit calls its `WriteAsync` exactly
  as 1.9.0 did, and its answer stands. With no `IProblemDetailsService` registered, the toolkit
  throws as before.
- `AddGlobalExceptionHandling` also registers a writer, after the framework's own, that answers
  in the toolkit's form what the framework writes through `IProblemDetailsService` for such a
  request. With `AddProblemDetails()` alone, what the framework writes is unchanged.

What a service sees change for a request that accepts no JSON (`text/plain`,
`application/xml`, `text/html` alone):
- the toolkit's handlers for API keys, HMAC and JWT: a 401 or 403 was a 500 (empty in
  Production, the developer exception page in Development) and is now a 401 or 403 problem
  document; a monitor will see the status change;
- the toolkit's exception handler, result helpers, status middlewares and 404 fallback: a 500
  (empty in Production, the developer exception page in Development) is now a problem document
  with the intended status;
- the logs, with `AddGlobalExceptionHandling`: under Kestrel one such exception gave four Error
  records and now gives one, the toolkit's. The three that are gone are the exception-handler
  middleware's two ("An exception was thrown attempting to execute the error handler.", "An
  unhandled exception has occurred while executing the request.") and Kestrel's own;
- the logs, with `AddProblemDetails()` alone: each failing helper, middleware, fallback or
  challenge gave one Kestrel Error record and now gives none;
- with `AddGlobalExceptionHandling`, `UseStatusCodePages()`: the plain text "Status Code: 404;
  Not Found" is now a problem document;
- with `AddGlobalExceptionHandling`, the developer exception page in Development, for an
  exception the toolkit's handler does not take: the plain-text page is now a problem document
  with the same content. The HTML page for `text/html` and for a browser is unchanged, with one
  exception: with the toolkit's handler in Development, a request for `text/html` alone now
  gets the JSON 500 where it got the HTML page after the handler had failed;
- with `AddGlobalExceptionHandling`, the following give the toolkit's form instead of the
  framework's bare body: `Results.ValidationProblem` and `TypedResults.ValidationProblem`,
  `Results.Problem` and `TypedResults.Problem`, and the filter of `AddValidation()`;
- with `AddGlobalExceptionHandling`, the minimal API's own 400 for a body that is not valid
  JSON: on 1.9.0 it was an empty 400 in Production, and in Development an exception that
  reached the exception handler, where it failed as above. It is now a 400 problem document
  in the toolkit's form.

A client that accepts JSON sees none of it. That is a request with no `Accept` header, with
`application/json`, `application/problem+json` or `*/*`, or with a browser's usual header,
which holds `*/*`. With the framework's `IProblemDetailsService` its answers are the same as
1.9.0's, byte for byte.

With an application's own `IProblemDetailsService` that does not ask the registered
`IProblemDetailsWriter`s, every answer is the same as 1.9.0's, byte for byte, whatever the
request accepts. One that hands the problem to the registered writers, as a service that wraps
the framework's own does, keeps its answers for a request that accepts JSON; with
`AddGlobalExceptionHandling`, a request that accepts no JSON is then answered by the writer
registered there, as a problem document, where 1.9.0 answered a 500.

## 2026-11-16 — .NET 11 target, .NET 10 dropped

### Removed (BREAKING for consumers on .NET 10)
- The `net10.0` target. All five packages target `net11.0` alone, so a project on .NET 10
  cannot take these versions. The last versions that support .NET 10:
  - `AmasiaLabs.Toolkit.MinimalApi` 1.8.3
  - `AmasiaLabs.Toolkit.Podman` 1.0.23
  - `AmasiaLabs.Toolkit.FlowflakeId` 1.4.21
  - `AmasiaLabs.Toolkit.FlowflakeId.Abstractions` 1.4.19
  - `AmasiaLabs.Toolkit.FlowflakeId.Extensions` 1.4.22

### Changed
- The packages target `net11.0` and go with the .NET 11 packages:
  `Microsoft.AspNetCore.OpenApi`, `Microsoft.AspNetCore.Authentication.JwtBearer`,
  `Microsoft.Extensions.Hosting` and `Microsoft.Extensions.Options.DataAnnotations` 11.0.0,
  and (MinimalApi) `Microsoft.OpenApi` `[3.11.0, 4.0.0)`, the 3.x line that
  `Microsoft.AspNetCore.OpenApi` 11.x is built against. No API change.
- `FlowflakeId` no longer lists `Microsoft.Extensions.Options`, and
  `FlowflakeId.Abstractions` no longer lists `System.ComponentModel.Annotations`: the .NET 11
  shared framework carries both.
- `AmasiaLabs.Toolkit.MinimalApi` 1.8.3 → 1.9.0.
- `AmasiaLabs.Toolkit.Podman` 1.0.23 → 1.1.0.
- `AmasiaLabs.Toolkit.FlowflakeId` 1.4.21 → 1.5.0.
- `AmasiaLabs.Toolkit.FlowflakeId.Abstractions` 1.4.19 → 1.5.0.
- `AmasiaLabs.Toolkit.FlowflakeId.Extensions` 1.4.22 → 1.5.0.

### Changed (build)
- `Directory.Build.props` targets `net11.0`; `global.json` pins the .NET 11 SDK (11.0.100,
  `latestMinor`); the workflows install the 11.0.x SDK.

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


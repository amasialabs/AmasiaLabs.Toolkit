# Changelog

All notable changes to this project will be documented in this file.

The format is based on Keep a Changelog (https://keepachangelog.com/en/1.1.0/),
and this project adheres to Semantic Versioning (https://semver.org/spec/v2.0.0.html).

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


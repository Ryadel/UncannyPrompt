# UncannyPrompt.UnitTests

`UncannyPrompt.UnitTests` is the fast behavioral safety net for the solution. It verifies application services, security primitives, API key authentication behavior, variable resolution, prompt versioning, and access-control hardening.

## Responsibilities

- Lock down application service behavior.
- Verify authorization and ACL edge cases.
- Verify prompt versioning and restore behavior.
- Verify variable resolution precedence.
- Verify security primitives and API key authentication paths.
- Keep tests fast enough to run on every local build.

## Contents

| Folder | Purpose |
| ------ | ------- |
| `Application/` | service-level tests |
| `Security/` | security primitive and API key auth tests |
| `TestSupport/` | reusable data seeding, test database, and configuration helpers |

## Current focus areas

- `PromptServiceTests`
- `PromptVersionServiceTests`
- `PromptResolutionServiceTests`
- `UserServiceTests`
- `AccessControlHardeningTests`
- `ApiKeyHasherTests`
- `ApiKeyAuthenticationHandlerTests`

## Running tests

```bash
dotnet test UncannyPrompt.slnx
```

Or, from the test project:

```bash
dotnet test tests/UncannyPrompt.UnitTests/UncannyPrompt.UnitTests.csproj
```

## Test design

Tests use the real application services and infrastructure implementations where practical. That gives confidence that behavior and DI wiring stay aligned with production code.

New authorization paths should add tests to `AccessControlHardeningTests`. New prompt or variable behavior should be covered at the application service level.

## What does not belong here

- browser/UI automation;
- full HTTP pipeline tests;
- production load tests;
- long-running integration tests that require external infrastructure.

Those should live in dedicated integration or end-to-end test projects when introduced.

## Related docs

- [Development](../../wiki/Development.md)
- [Sharing and Access Control](../../wiki/Sharing-and-Access-Control.md)

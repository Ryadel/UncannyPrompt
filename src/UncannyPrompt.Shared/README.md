# UncannyPrompt.Shared

`UncannyPrompt.Shared` is a tiny leaf project for cross-project constants that are not domain concepts and should not force dependency cycles.

## Responsibilities

- Hold stable constants used by multiple layers.
- Stay dependency-free.
- Avoid becoming a generic utilities bucket.

## Contents

| File | Purpose |
| ---- | ------- |
| `AppConstants.cs` | authentication scheme names and other well-known application constants |

## Dependency rules

Shared should have no project references and no NuGet packages.

Allowed:

- simple constants;
- tiny pure helpers when there is no better layer.

Not allowed:

- business entities or enums;
- application services;
- infrastructure clients;
- HTTP middleware;
- anything that requires a dependency.

If a helper grows behavior, options, or dependencies, move it to the proper layer before `Shared` becomes a second domain model.

## Related docs

- [Architecture](../../wiki/Architecture.md)

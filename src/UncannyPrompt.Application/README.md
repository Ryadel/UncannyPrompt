# UncannyPrompt.Application

`UncannyPrompt.Application` is the use-case layer. It defines what the product does: creating prompts, versioning content, resolving variables, applying access control, managing shares, auditing events, and exposing DTOs to the WebApp.

## Responsibilities

- Define application service interfaces and implementations.
- Define DTOs and request records used by the WebApp.
- Own authorization decisions that are part of business behavior.
- Compose repository queries without taking a dependency on a concrete `DbContext`.
- Define infrastructure-facing contracts such as repositories, unit of work, secret protection, and token hashing.

## Contents

| Folder | Purpose |
| ------ | ------- |
| `Abstractions/` | service, repository, security, unit-of-work, and context interfaces |
| `Services/` | prompt, sharing, tenant, variable, search, audit, tag, folder, project, and user use cases |
| `Dtos/` | data returned across the application boundary |
| `Requests/` | input models accepted by controllers/pages |
| `Mapping/` | DTO mapping helpers and projection utilities |
| `DependencyInjection/` | `AddApplicationServices()` registration |

## Important services

- `PromptService` - prompt CRUD, tags, favorites, pins, copy logs, version creation.
- `PromptVersionService` - version history and restore.
- `PromptResolutionService` - variable substitution with explicit precedence.
- `SharingService` - grants, public links, token lookup, public prompt access.
- `TenantScopeService` - project/folder/prompt access checks.
- `PermissionService` - maps roles and share levels to concrete application permissions.
- `AccessControlQueries` - SQL-translatable authorization filters for listings.
- `AuditService` - append-only audit events.

## Dependency rules

Application depends on Domain and Shared. It may use framework abstractions needed for composition and async query execution, but it must not depend on concrete infrastructure clients.

Allowed:

- business use cases;
- authorization logic;
- DTOs and request contracts;
- interfaces implemented by Infrastructure.

Not allowed:

- EF Core `DbContext` implementations;
- SQL migrations;
- ASP.NET Core controllers, pages, filters, handlers;
- vendor SDK clients.

## Related docs

- [Prompt Lifecycle](../../wiki/Prompt-Lifecycle.md)
- [Variables and Resolution](../../wiki/Variables-and-Resolution.md)
- [Sharing and Access Control](../../wiki/Sharing-and-Access-Control.md)

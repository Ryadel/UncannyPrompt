# UncannyPrompt.Domain

`UncannyPrompt.Domain` is the pure domain core. It owns the business vocabulary used by the rest of the system: users, tenants, workspaces, projects, folders, prompts, versions, variables, shares, public links, audit events, and the enums that describe them.

## Responsibilities

- Define persistent entities and domain enums.
- Keep the product hierarchy explicit: `Tenant -> Workspace -> Project -> Folder -> Prompt`.
- Provide the shared `Entity` base type and `ISoftDeletable` marker.
- Stay free of infrastructure, HTTP, persistence, logging, and framework dependencies.

## Contents

| Area | Types |
| ---- | ----- |
| Identity | `User`, `AuthIdentity`, `UserApiKey` |
| Tenancy | `Tenant`, `TenantMembership`, `Workspace` |
| Organization | `Project`, `Folder` |
| Prompt authoring | `Prompt`, `PromptVersion`, `PromptNote`, `VersionLabel` |
| Metadata | `Tag`, `PromptTag`, `Favorite` |
| Variables | `VariableDefinition`, `VariableValue` |
| Sharing | `ShareGrant`, `PublicShareLink` |
| Audit | `AuditEvent` |
| Enums | `TenantRole`, `ApplicationPermission`, `VariableScope`, `SharePermission`, `PromptStatus`, and related value sets |

## Dependency rules

This project should not reference any other project or NuGet package. If a change requires EF Core attributes, ASP.NET Core types, logging APIs, or provider SDKs, it belongs outside Domain.

Allowed:

- business entities;
- business enums;
- tiny domain-level marker interfaces.

Not allowed:

- EF Core mapping configuration;
- DTOs and request models;
- repositories and services;
- authentication, authorization handlers, middleware;
- cryptography and hashing implementations.

## Related docs

- [Architecture](../../wiki/Architecture.md)
- [Storage and Persistence](../../wiki/Storage-and-Persistence.md)

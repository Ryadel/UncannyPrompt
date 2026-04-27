# UncannyPrompt

**UncannyPrompt is a self-hostable .NET 10 application for organizing, versioning, sharing, resolving, and copying AI prompts across teams and workspaces.** It gives prompt-heavy teams a central place to manage reusable prompt templates, variables, access rules, public links, and audit trails instead of scattering critical prompt knowledge across chats, documents, and personal notes.

UncannyPrompt ships as a Clean Architecture monolith: one ASP.NET Core Razor Pages WebApp backed by SQL Server. It is intentionally small enough to run with Docker Compose, while keeping explicit boundaries for future growth such as OpenSearch-backed search, background workers, cache, queues, or object storage.

## What it does

- **Prompt catalog** - stores prompts by tenant, workspace, project, and folder, with tags, favorites, pins, notes, and status metadata.
- **Version history** - records immutable prompt content versions when content changes or explicit version metadata is provided, with restore support.
- **Variable resolution** - resolves prompt placeholders through scoped variables using an explicit precedence ladder.
- **Team access control** - combines tenant roles with project, folder, and prompt ACL grants, including inheritance.
- **Public sharing** - exposes selected prompts through revocable and expiring public links with indexed token lookup.
- **Federated login** - supports cookie sessions, API keys, Google OAuth, Microsoft Entra ID, and GitHub OAuth, each enabled only when configured.
- **Operational hardening** - includes CSRF protection, rate limiting, authenticated Swagger, security headers, audit events, Serilog, OpenTelemetry, health checks, and Docker-ready configuration.

## Why it was built

Prompts often become part of real operational knowledge: support playbooks, coding workflows, product analysis, legal review checklists, sales enablement, and internal automation. When those prompts live in personal chats or loose documents, teams lose version history, access control, reviewability, and reuse.

UncannyPrompt was built to close that gap:

- **Prompt knowledge should be shared deliberately.** Teams need folders, tags, projects, variables, and role-aware sharing instead of copy-pasted text fragments.
- **Prompt changes should be traceable.** Important prompts need version history and restore behavior, not silent overwrites.
- **Sensitive prompt material should stay self-hosted.** The application can run in the organization's own infrastructure with SQL Server as the system of record.
- **Architecture should leave room to grow.** Domain, Application, Infrastructure, WebApp, Shared, and Tests are separated so new storage/search/auth implementations can be added without rewriting the product core.

## Quick start

```bash
cp .env.example .env
docker compose up --build
```

The WebApp is exposed at:

```text
http://localhost:8080
```

For local development with SQL Server in Docker and the WebApp running from the IDE:

```bash
cp .env.example .env
cp .env.local.sample .env.local
docker compose up -d sqlserver
dotnet ef database update --project src/UncannyPrompt.Infrastructure --startup-project src/UncannyPrompt.WebApp
dotnet run --project src/UncannyPrompt.WebApp --urls http://localhost:5088
```

## Tech stack

.NET 10, ASP.NET Core Razor Pages, EF Core 10, SQL Server, Tailwind CSS, DaisyUI, Alpine.js, Microsoft.Identity.Web, Google OAuth, GitHub OAuth, Serilog, OpenTelemetry, xUnit, Docker Compose.

## Repository layout

```text
src/
  UncannyPrompt.Domain          pure domain entities and enums
  UncannyPrompt.Application     use cases, DTOs, interfaces, authorization logic
  UncannyPrompt.Infrastructure  EF Core, SQL Server, migrations, crypto, repositories
  UncannyPrompt.WebApp          Razor Pages, API controllers, auth, HTTP pipeline
  UncannyPrompt.Shared          small shared constants

tests/
  UncannyPrompt.UnitTests       service, security, and access-control tests

wiki/
  developer and operator documentation, intended for GitHub Wiki publishing
```

## Documentation

Developer and operator documentation lives in [wiki/](wiki/):

- **[Home](wiki/Home.md)** - table of contents
- **[Architecture](wiki/Architecture.md)** - layers, runtime topology, domain model, entry points
- **[Configuration](wiki/Configuration.md)** - `appsettings`, `.env`, `.env.local`, environment variables, derived values
- **[Authentication and Authorization](wiki/Authentication-and-Authorization.md)** - cookies, API keys, OAuth/OIDC, roles, ACLs
- **[Prompt Lifecycle](wiki/Prompt-Lifecycle.md)** - creation, versioning, restore, favorites, copy logging, search
- **[Variables and Resolution](wiki/Variables-and-Resolution.md)** - variable scopes, precedence, secret handling
- **[Sharing and Access Control](wiki/Sharing-and-Access-Control.md)** - grants, inheritance, public links, audit
- **[Storage and Persistence](wiki/Storage-and-Persistence.md)** - SQL Server, EF Core, repositories, migrations
- **[Security](wiki/Security.md)** - CSRF, rate limiting, headers, tokens, secrets, audit
- **[Observability](wiki/Observability.md)** - Serilog, OpenTelemetry, health checks, operational signals
- **[Development](wiki/Development.md)** - setup, Docker, migrations, tests, CSS build
- **[Deployment](wiki/Deployment.md)** - Docker Compose, production checklist, scaling, backup

## Common commands

```bash
dotnet build UncannyPrompt.slnx
dotnet test UncannyPrompt.slnx
docker compose config --no-interpolate
docker compose up --build
```

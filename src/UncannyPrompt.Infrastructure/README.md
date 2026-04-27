# UncannyPrompt.Infrastructure

`UncannyPrompt.Infrastructure` is the technical implementation layer. It turns Application abstractions into concrete behavior using EF Core, SQL Server, migrations, repositories, unit of work, configuration composition, and security primitives.

## Responsibilities

- Configure EF Core and SQL Server persistence.
- Implement repository and unit-of-work abstractions.
- Hold EF Core migrations and model snapshots.
- Implement secret protection and token hashing primitives.
- Compose derived configuration values such as connection strings and Entra ID authority.
- Register infrastructure services through one DI extension.

## Contents

| Folder | Purpose |
| ------ | ------- |
| `Persistence/` | `UncannyPromptDbContext`, `EfRepository<T>`, `EfUnitOfWork` |
| `Migrations/` | EF Core migration history |
| `Security/` | `AesSecretProtector`, `ApiKeyHasher`, `PublicTokenHasher` |
| `Configuration/` | `.env` loader and derived configuration composer |
| `DependencyInjection/` | `AddInfrastructure(IConfiguration)` registration |

## Configuration

The WebApp calls `ConfigureUncannyPromptHost()`, which loads `.env`, optionally overlays `.env.local`, restores process environment precedence, and derives:

- `ConnectionStrings:UncannyPrompt` from `Database:*`;
- `Authentication:EntraId:Authority` from `Authentication:EntraId:Instance` and `Authentication:EntraId:TenantId`.

See [Configuration](../../wiki/Configuration.md).

## Persistence

SQL Server is the system of record. The DbContext maps domain entities, stamps lifecycle timestamps, applies indexes, and owns schema evolution through migrations.

Migration commands:

```bash
dotnet ef database update --project src/UncannyPrompt.Infrastructure --startup-project src/UncannyPrompt.WebApp
dotnet ef migrations add <MigrationName> --project src/UncannyPrompt.Infrastructure --startup-project src/UncannyPrompt.WebApp --output-dir Migrations
```

## Dependency rules

Infrastructure depends on Application, Domain, and Shared. It must not be referenced by Domain or Application.

Allowed:

- EF Core mappings and migrations;
- concrete repository implementations;
- SQL Server setup;
- crypto and hashing implementations;
- configuration helpers.

Not allowed:

- business use-case decisions;
- HTTP controllers and Razor Pages;
- UI or request-binding concerns.

## Related docs

- [Configuration](../../wiki/Configuration.md)
- [Storage and Persistence](../../wiki/Storage-and-Persistence.md)
- [Security](../../wiki/Security.md)

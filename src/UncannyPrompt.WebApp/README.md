# UncannyPrompt.WebApp

`UncannyPrompt.WebApp` is the executable ASP.NET Core host. It serves the Razor Pages UI, exposes REST controllers, configures authentication and authorization, applies HTTP hardening, serves static assets, and composes the Application and Infrastructure layers.

## Responsibilities

- Act as the composition root for dependency injection and middleware.
- Host Razor Pages for the user interface.
- Host API controllers for prompt, folder, project, variable, tag, share, public link, audit, and account operations.
- Configure cookie auth, API key auth, Google OAuth, Microsoft Entra ID, and GitHub OAuth.
- Enforce antiforgery, rate limiting, security headers, authenticated Swagger, and health checks.
- Build and serve Tailwind/DaisyUI assets.

## Contents

| Area | Purpose |
| ---- | ------- |
| `Program.cs` | host bootstrap, DI, auth, security middleware, OpenTelemetry, endpoints |
| `Extensions/` | host configuration setup, including `.env` loading and Serilog |
| `Controllers/` | JSON API endpoints and controller request records |
| `Pages/` | Razor Pages UI |
| `Security/` | API key handler, antiforgery filter, current user context |
| `Styles/` | Tailwind input CSS |
| `wwwroot/` | compiled static assets, favicon, libraries |
| `package.json` | frontend CSS build scripts |

## Main pages

- `Index` - main prompt workspace.
- `Onboarding` - first-use/onboarding flow.
- `Projects` - project explorer.
- `Variables` - variable management.
- `Tags` - tag management.
- `Shares` - sharing management.
- `Versions` - version history.
- `Audit` - audit review.
- `Settings` - settings surface.
- `PublicPrompt` - anonymous public prompt view.
- `Account/Login` - login provider entry point.

## Authentication

Providers are registered conditionally. A missing external provider configuration does not break startup; the provider simply remains unavailable.

Configured schemes:

- cookie auth for browser sessions;
- API key auth for programmatic access;
- Google OAuth;
- Microsoft Entra ID through `Microsoft.Identity.Web`;
- GitHub OAuth.

## Frontend assets

```bash
cd src/UncannyPrompt.WebApp
npm install
npm run css:build
```

During UI work:

```bash
npm run css:watch
```

## Dependency rules

WebApp may reference Application, Infrastructure, and Shared. It should not contain business rules that belong in Application.

Allowed:

- request binding;
- HTTP responses;
- Razor UI;
- authentication handlers and filters;
- middleware setup;
- static assets.

Not allowed:

- direct EF Core queries from controllers/pages;
- authorization rules implemented only in UI/controller code;
- business workflows that bypass application services.

## Related docs

- [Architecture](../../wiki/Architecture.md)
- [Authentication and Authorization](../../wiki/Authentication-and-Authorization.md)
- [Security](../../wiki/Security.md)
- [Development](../../wiki/Development.md)

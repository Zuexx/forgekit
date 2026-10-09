# ForgeKit — Project Structure

## Overview

```
forgekit/
├── api/                 # C# .NET 10 backend
├── app/                 # Next.js frontend
├── docs/                # Guides, ADRs, implementation plans
├── openspec/            # Specifications and change proposals
├── scripts/             # preflight, verify, sync-workflow
├── .githooks/           # pre-push check for plan → OpenSpec task citations
├── .template.config/    # `dotnet new forgekit` template definition
├── data/                # Default shared SQLite database (created on first run)
├── compose.yaml         # Optional local Postgres
└── package.json         # Workflow tooling only (OpenSpec, CodeGraph, grillme)
```

---

## API (`/api`) — C# .NET 10

### Solution
```
api/
├── ForgeKit.sln
├── AGENTS.md
├── README.md
├── Directory.Packages.props          # Central package versions
├── Directory.Build.targets           # Build-wide MSBuild targets
├── Anvil/                            # Shared layer — never renamed by the template
├── Anvil.Tests/                      # Shared-layer tests — also never renamed
├── ForgeKit.Api/                     # Product layer — renamed per product
├── ForgeKit.Api.Migrations.Sqlite/
├── ForgeKit.Api.Migrations.Postgres/
├── ForgeKit.Api.Migrations.SqlServer/
└── ForgeKit.Api.Tests/               # Product tests
```

### Two-Layer Boundary

The API is split so that base updates can reach generated products.

| | `Anvil/` | `<Product>.Api/` |
|---|---|---|
| Renamed by the template | No | Yes |
| Contents across products | Identical | Product-specific |
| Holds | Result/exception types, middlewares, MediatR behaviors, Unit of Work, `PlatformDbContext`, module and DI plumbing, auth entities | `Program.cs`, product entities, modules, services, `AppDbContext` |

A file belongs in `Anvil/` only if it would be byte-identical in every product. The
compile-time guarantee is that `Anvil` builds without referencing the product project;
if a shared file starts needing a product type, the dependency is inverted through an
interface instead — see `IDataSeeder`.

Because `Anvil/` paths match the base exactly, a product receives shared-layer updates
with `git checkout upstream/main -- api/Anvil api/Anvil.Tests`. See `docs/FORKING_GUIDE.md`.

### `Anvil/` — Shared Layer

```
Anvil/
├── Behaviors/
│   └── ValidationBehavior.cs          # MediatR pipeline validation
├── Constants/
│   ├── AppSettingKeys.cs
│   └── ErrorCodes.cs
├── Data/
│   ├── PlatformDbContext.cs           # Base DbContext: soft-delete filters, audit stamping
│   ├── UnitOfWork.cs
│   └── Auth/
│       └── BetterAuthDbContext.cs     # Better Auth's tables, read by the API
├── Domain/Services/
│   └── SoftDeleteDomainService.cs
├── Entities/
│   ├── Base/                          # BaseEntity, IAuditableEntity, ISoftDelete
│   └── Auth/                          # User, Session, Account, Jwk, Verification
├── Exceptions/                        # BusinessLogic, Conflict, Domain, InvalidState, NotFound, Unauthorized, ValidationApp
├── Extensions/
│   ├── ConfigureJwtBearerOptions.cs
│   ├── CorsExtensions.cs
│   ├── DatabaseProviderExtensions.cs  # Provider selection, SQLite pragmas
│   ├── HttpContextAccessorExtension.cs
│   ├── ModuleExtension.cs             # Module discovery (RegisterModules)
│   ├── PlatformServiceExtensions.cs   # Shared-layer DI registrations
│   └── ResultEndpointExtensions.cs
├── Foundations/
│   └── JwksProvider.cs                # Signing keys for JWT bearer validation
├── Handlers/
│   ├── ResultCommandHandler.cs
│   └── ResultQueryHandler.cs
├── Interfaces/
│   ├── IAuditContext.cs
│   ├── IDataSeeder.cs                 # Implemented by the product; see the boundary above
│   ├── IJwksProvider.cs
│   ├── IModule.cs / IRootModule.cs / ISampleModule.cs
│   └── IUnitOfWork.cs
├── Middlewares/
│   ├── CorrelationIdMiddleware.cs
│   └── ExceptionHandlingMiddleware.cs
├── Models/
│   ├── AuthorizedUser.cs
│   ├── ErrorResponse.cs
│   └── JwtSetupData.cs
├── Modules/
│   └── HealthModule.cs
├── Results/
│   ├── Result.cs
│   └── ResultExtensions.cs
└── Services/
    └── AuditContextService.cs
```

### `ForgeKit.Api/` — Product Project

```
ForgeKit.Api/
├── Program.cs                         # Entry point & DI registration
├── appsettings.json
├── appsettings.Development.json
├── appsettings.Local.json.example     # Copy to appsettings.Local.json (ignored) for secrets
├── ForgeKit.Api.http                  # Manual API testing
├── Properties/launchSettings.json
│
├── Data/
│   └── AppDbContext.cs                # Product DbContext (derives from Anvil's PlatformDbContext)
│
├── Entities/                          # Sample TODO domain
│   ├── Core/                          # Member, Workspace
│   ├── Analytics/                     # DailyActivitySnapshot, WorkspaceAnalytics
│   ├── Configuration/                 # Category, CategoryLabel, Label
│   └── Todos/                         # TodoItem, TodoStatusHistory
│
├── Extensions/
│   └── ServiceExtension.cs            # Product DI registrations (RegisterApplicationServices)
│
├── Foundations/
│   └── PocDataSeeder.cs               # IDataSeeder implementation
│
├── Modules/
│   └── SampleResourceModule.cs        # Non-production endpoint convention sample
│
├── Samples/                           # Sample CQRS handlers & validators
│
└── Services/
    └── Todos/
        └── TodoService.cs             # Reference domain service for persistence conventions
```

See [SAMPLES.md](SAMPLES.md) for the starter-kit sample inventory and removal guidance.

### Migration Projects

Each `ForgeKit.Api.Migrations.<Provider>/` project contains provider-specific design-time factories and independent `AppDbContext` and `BetterAuthDbContext` migration chains. These projects are CLI and deployment artifacts; they do not run as separate services.

```
ForgeKit.Api.Migrations.<Provider>/
├── DesignTimeDbContextFactories.cs
└── Migrations/
    ├── App/
    └── Auth/
```

### Test Projects

`Anvil.Tests/` tests the shared layer with types defined in the tests themselves, and must not
reference the product. `ForgeKit.Api.Tests/` covers the product, and the shared-layer behavior
that only shows up through the product's composition root.

```
ForgeKit.Api.Tests/
├── Data/                              # Model, migrations, schema separation, UnitOfWork
├── Domain/Services/                   # SoftDelete service tests
├── Exceptions/                        # Exception type tests
├── Extensions/                        # Database provider & SQLite pragma tests
├── Handlers/                          # ResultHandler tests
├── Integration/                       # WebApplicationFactory + integration tests
├── Middlewares/                       # Middleware unit tests
├── Modules/                           # Health, discovery & sample module tests
├── Results/                           # Result type tests
├── Samples/                           # Sample handler tests
├── Services/                          # AuditContext & Todo service tests
└── Validators/                        # FluentValidation tests
```

### `docs/` — Documentation (repository root)

```
docs/
├── STRUCTURE.md / FORKING_GUIDE.md / LOCAL_DEVELOPMENT.md
├── DEPENDENCY_CONSTRAINTS.md / QUALITY_GATES.md / SAMPLES.md
├── SECRET_INCIDENT_RESPONSE.md
├── api/
│   ├── USER_GUIDE.md
│   ├── EXTENDING_THE_API.md
│   ├── EXCEPTION_HANDLING_GUIDE.md / ADR_EXCEPTION_HANDLING.md
│   ├── FLUENT_VALIDATION_GUIDE.md
│   ├── RESULT_PATTERN_GUIDE.md
│   ├── CONFIGURATION_GUIDE.md
│   ├── COMMIT_CONVENTION.md
│   ├── API_ERRORS.md
│   ├── XML_DOCUMENTATION_GUIDE.md
│   ├── logging.md
│   └── GLOSSARY.md
├── adr/                               # Architecture Decision Records (001–008)
└── superpowers/plans/                 # Implementation plans citing OpenSpec task ids
```

### `openspec/` — Feature Change Tracking (repository root)

```
openspec/
├── config.yaml                        # Schema + project context for planning
├── specs/                             # Accepted capability specs
└── changes/                           # One folder per change; shipped ones move to archive/
    ├── <feature>/
    │   ├── proposal.md
    │   ├── design.md
    │   ├── tasks.md
    │   └── specs/<area>/spec.md
    └── ...
```

---

## App (`/app`) — Next.js (TypeScript)

### Root Config
```
app/
├── package.json / pnpm-lock.yaml / pnpm-workspace.yaml
├── next.config.ts
├── proxy.ts                           # Next.js proxy entry; policy lives in proxies/
├── tsconfig.json
├── eslint.config.mjs
├── postcss.config.mjs
├── vitest.config.mts / vitest.server-only-stub.ts
├── playwright.config.ts
├── e2e/                               # Playwright end-to-end tests (auth flow)
├── hooks/                             # Shared React hooks (use-mobile)
└── components.json                    # shadcn/ui config
```

### App Router (`/app/app`)

```
app/
├── globals.css
├── [locale]/                          # i18n root
│   ├── layout.tsx
│   ├── page.tsx
│   ├── (admin)/
│   │   └── layout.tsx                 # Full sidebar shell scaffold; no page.tsx yet
│   ├── (authenticate)/
│   │   ├── sign-in/page.tsx
│   │   └── sign-up/page.tsx
│   └── (user)/
│       └── layout.tsx                 # Header-only shell scaffold; no page.tsx yet
└── api/
    ├── [[...hono]]/route.ts           # Hono RPC handler
    └── auth/[[...all]]/route.ts       # Better Auth handler
```

### `components/` — UI Components

```
components/
├── app-sidebar.tsx                    # Sample sidebar shell; stock shadcn team/nav/project data, not wired to real routes
├── nav-breadcrumb.tsx / nav-main.tsx / nav-projects.tsx / nav-user.tsx
├── team-switcher.tsx / locale-switcher.tsx / theme-switcher.tsx
├── radial-menu.tsx
├── logo.tsx                           # Inline <Logo>/<LogoMark>, fill="currentColor" for theme reactivity
├── user-menu.tsx / user-menu-content.tsx
├── box.tsx
├── form-fields/
│   ├── input-field.tsx
│   └── password-field.tsx
└── ui/                                # shadcn/ui primitives
    └── (avatar, badge, button, card, input, sidebar, ...)
```

### `features/` — Feature Modules

```
features/
└── authenticate/
    ├── index.ts
    ├── route.ts                       # Hono route definitions
    ├── components/                    # sign-in-card, sign-up-card
    ├── hooks/                         # use-sign-in, use-sign-up, use-sign-out, use-social-sign-in, use-me
    └── schemas/                       # Zod schemas: sign-in, sign-up
```

### `lib/` — Core Library

```
lib/
├── utils.ts
├── auth-client.ts                     # Better Auth client
├── auth.config.ts                     # Guarded `server-only` wrapper around auth-instance.ts
├── auth-instance.ts                   # Real Better Auth config; composes whichever provider
│                                       # Database__Provider selects
├── db/
│   ├── sqlite.ts / sqlite-instance.ts       # Default provider — no external service needed
│   ├── postgres.ts / postgres-instance.ts   # Optional provider
│   └── mssql.ts / mssql-instance.ts         # Optional provider
├── rpc/
│   ├── rpc-client.ts                  # Hono RPC typed client
│   └── session-middleware.ts
├── queries/
│   ├── hooks/                         # use-api-query, paginated, infinite, dependent
│   └── mutations/                     # use-api-mutation
└── store/
    ├── index.ts / hooks.ts / types.ts
    └── slices/
        ├── ui.slice.ts
        └── user.slice.ts
```

### `providers/` — React Context Providers

```
providers/
├── app-provider.tsx                   # Root provider tree
├── query-provider.tsx                 # TanStack Query
├── store-provider.tsx                 # Zustand store
└── translation-provider.tsx           # next-intl
```

### `proxies/` — Server-side Policy / Auth Proxy

```
proxies/
├── create-proxy.ts
├── evaluate-policy.ts
├── resolve-context.ts
├── actions.ts
└── types.ts
```

### `i18n/` — Internationalization Config

```
i18n/
├── config.ts                          # Supported locales: en, zh-TW
├── routing.ts
└── request.ts
```

### `messages/` — Translations

```
messages/
├── en/          # auth, common, form, toast, validation
└── zh-TW/
```

### `constants/`

```
constants/
├── routes.ts
├── cookies.ts
└── breadcrumb-keys.ts
```

### `types/`

```
types/
├── auth.d.ts
├── next-intl.d.ts
└── style.d.ts
```

---

## Key Technology Stack

| Layer | Technology |
|---|---|
| Backend framework | ASP.NET Core 10 (Minimal APIs) |
| ORM | Entity Framework Core |
| CQRS | MediatR |
| Validation | FluentValidation |
| Auth (API) | JWT + JWKS |
| Frontend framework | Next.js 16 (App Router) |
| Language | TypeScript |
| Styling | Tailwind CSS + shadcn/ui |
| State management | Zustand |
| Server state | TanStack Query |
| Auth (App) | Better Auth |
| RPC | Hono (typed client/server) |
| i18n | next-intl (en / zh-TW) |
| Package manager | pnpm |

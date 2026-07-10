# ForgeKit — Project Structure

## Overview

```
forgekit/
├── api/    # C# .NET 10 backend
└── app/    # Next.js frontend
```

---

## API (`/api`) — C# .NET 10

### Solution
```
api/
├── ForgeKit.sln
├── AGENTS.md
├── README.md
├── .github/prompts/          # AI prompt templates (openspec)
├── docs/                     # Guides & ADRs
├── openspec/                 # Feature specs & changes
├── ForgeKit.Api/             # Main project
├── ForgeKit.Api.Migrations.Sqlite/
├── ForgeKit.Api.Migrations.Postgres/
├── ForgeKit.Api.Migrations.SqlServer/
└── ForgeKit.Api.Tests/       # Test project
```

### `ForgeKit.Api/` — Main Project

```
ForgeKit.Api/
├── Program.cs                         # Entry point & DI registration
├── appsettings.json
├── appsettings.Development.json
├── ForgeKit.Api.http                  # Manual API testing
├── Properties/launchSettings.json
│
├── Behaviors/
│   └── ValidationBehavior.cs          # MediatR pipeline validation
│
├── Constants/
│   ├── AppSettingKeys.cs
│   └── ErrorCodes.cs
│
├── Data/
│   ├── AppDbContext.cs                # Main EF Core DbContext
│   ├── UnitOfWork.cs
│   └── Auth/
│       └── BetterAuthDbContext.cs     # Auth-specific context
│
├── Domain/
│   └── Services/
│       └── SoftDeleteDomainService.cs
│
├── Entities/
│   ├── Base/
│   │   ├── BaseEntity.cs
│   │   ├── IAuditableEntity.cs
│   │   └── ISoftDelete.cs
│   ├── Auth/                          # User, Session, Account, Jwk, Verification
│   ├── Core/                          # Member, Workspace
│   ├── Analytics/                     # DailyActivitySnapshot, WorkspaceAnalytics
│   ├── Configuration/                 # Category, CategoryLabel, Label
│   └── Todos/                         # TodoItem, TodoStatusHistory
│
├── Exceptions/                        # BusinessLogic, Conflict, Domain, NotFound, Unauthorized, Validation
│
├── Extensions/
│   ├── ConfigureJwtBearerOptions.cs
│   ├── CorsExtensions.cs
│   ├── HttpContextAccessorExtension.cs
│   ├── ModuleExtension.cs
│   ├── ResultEndpointExtensions.cs
│   └── ServiceExtension.cs
│
├── Foundations/
│   ├── JwksProvider.cs
│   └── PocDataSeeder.cs
│
├── Handlers/
│   ├── ResultCommandHandler.cs
│   └── ResultQueryHandler.cs
│
├── Interfaces/
│   ├── IAuditContext.cs
│   ├── IJwksProvider.cs
│   ├── IModule.cs / IRootModule.cs / ISampleModule.cs
│   └── IUnitOfWork.cs
│
├── Middlewares/
│   ├── CorrelationIdMiddleware.cs
│   └── ExceptionHandlingMiddleware.cs
│
├── Models/
│   ├── AuthorizedUser.cs
│   ├── ErrorResponse.cs
│   └── JwtSetupData.cs
│
├── Modules/
│   ├── HealthModule.cs
│   └── SampleResourceModule.cs          # Non-production endpoint convention sample
│
├── Results/
│   ├── Result.cs
│   └── ResultExtensions.cs
│
├── Samples/                           # Sample CQRS handlers & validators
│
└── Services/
    └── Todos/
        └── TodoService.cs              # Reference domain service for persistence conventions
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

### `ForgeKit.Api.Tests/` — Test Project

```
ForgeKit.Api.Tests/
├── Data/                              # UnitOfWork unit & audit tests
├── Domain/Services/                   # SoftDelete service tests
├── Exceptions/                        # Exception type tests
├── Handlers/                          # ResultHandler tests
├── Integration/                       # WebApplicationFactory + integration tests
├── Middlewares/                       # Middleware unit tests
├── Modules/                           # Health & Sample module tests
├── Results/                           # Result type tests
├── Samples/                           # Sample handler tests
├── Services/                          # AuditContext & Todo service tests
└── Validators/                        # FluentValidation tests
```

### `docs/` — Documentation

```
docs/
├── USER_GUIDE.md
├── EXTENDING_THE_API.md
├── EXCEPTION_HANDLING_GUIDE.md
├── FLUENT_VALIDATION_GUIDE.md
├── RESULT_PATTERN_GUIDE.md
├── CONFIGURATION_GUIDE.md
├── COMMIT_CONVENTION.md
├── API_ERRORS.md
├── XML_DOCUMENTATION_GUIDE.md
├── logging.md
├── GLOSSARY.md
└── adr/                               # Architecture Decision Records (001–007)
```

### `openspec/` — Feature Change Tracking

```
openspec/
├── project.md
├── specs/                             # Accepted capability specs
└── changes/                           # One folder per feature change
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
├── tsconfig.json
├── eslint.config.mjs
├── postcss.config.mjs
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
│   │   ├── layout.tsx
│   │   └── statisticals/regional-analytics/page.tsx
│   ├── (authenticate)/
│   │   ├── sign-in/page.tsx
│   │   └── sign-up/page.tsx
│   └── (user)/
│       ├── layout.tsx
│       └── healthcare-facility/page.tsx
└── api/
    ├── [[...hono]]/route.ts           # Hono RPC handler
    └── auth/[[...all]]/route.ts       # Better Auth handler
```

### `components/` — UI Components

```
components/
├── app-siderbar.tsx
├── nav-breadcrumb.tsx / nav-main.tsx / nav-projects.tsx / nav-user.tsx
├── team-switcher.tsx / locale-switcher.tsx / theme-switcher.tsx
├── radial-menu.tsx
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
    ├── hooks/                         # use-sign-in, use-sign-out, use-social-sign-in, use-me
    └── schemas/                       # Zod schemas: sign-in, sign-up
```

### `lib/` — Core Library

```
lib/
├── utils.ts
├── auth-client.ts                     # Better Auth client
├── auth.config.ts                     # Better Auth server config
├── db/
│   ├── postgres.ts                    # PostgreSQL connection pool
│   └── mssql.ts                       # Optional Kysely MSSQL helper
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
├── store-provider.tsx                 # Redux store
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
├── config.ts                          # Supported locales: en, ko-KR, zh-TW
├── routing.ts
└── request.ts
```

### `messages/` — Translations

```
messages/
├── en/          # auth, common, form, toast, validation
├── ko-KR/
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
| State management | Redux Toolkit |
| Server state | TanStack Query |
| Auth (App) | Better Auth |
| RPC | Hono (typed client/server) |
| i18n | next-intl (en / ko-KR / zh-TW) |
| Package manager | pnpm |

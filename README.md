# ForgeKit - Full-Stack Starter Kit

A full-stack starter kit demonstrating project conventions through a TODO service. It includes a .NET 10 API, a Next.js frontend, tests, architecture documentation, and OpenSpec-based change management.

## 🏗️ Architecture

```
forgekit/
├── api/          # C# .NET 10 Web API (ASP.NET Core)
│   ├── ForgeKit.sln
│   ├── ForgeKit.Api/       # Main project
│   └── ForgeKit.Api.Tests/ # Test project
│
└── app/          # Next.js 16 Frontend (TypeScript)
    ├── package.json
    └── app/      # App Router
```

**Communication:** HTTP REST API + Hono RPC typed client  
**Technologies are completely separate** — zero conflicts.

---

## 🚀 Quick Start

### Prerequisites
- **.NET 10** (for API)
- **Node.js 24** + **pnpm 11** (for App)
- **PostgreSQL** for the frontend's Better Auth database

The API uses SQLite by default and does not require an external database. PostgreSQL and SQL Server are optional API provider choices. The frontend currently uses Better Auth's PostgreSQL adapter and therefore needs PostgreSQL for authentication flows.

### API (Backend)

```bash
cd api
dotnet restore
dotnet build
dotnet tool restore
dotnet ef database update --project ForgeKit.Api.Migrations.Sqlite --startup-project ForgeKit.Api.Migrations.Sqlite --context AppDbContext
dotnet ef database update --project ForgeKit.Api.Migrations.Sqlite --startup-project ForgeKit.Api.Migrations.Sqlite --context BetterAuthDbContext
dotnet run --project ForgeKit.Api
```

API runs on `http://localhost:5000` or `https://localhost:7288` with Scalar docs at `/scalar/v1`.

To override local API configuration without committing credentials:

```bash
cp ForgeKit.Api/appsettings.Local.json.example ForgeKit.Api/appsettings.Local.json
```

Set `Database:Provider` to `Sqlite`, `Postgres`, or `SqlServer` and configure the matching connection string. EF Core migrations contain provider-specific DDL; ForgeKit ships isolated migration projects for all three providers. See the [configuration guide](docs/api/CONFIGURATION_GUIDE.md#database-configuration).

### App (Frontend)

```bash
cd app
pnpm install
cp .env.local.example .env.local
# Set DATABASE_URL and generate BETTER_AUTH_SECRET with: openssl rand -base64 32
pnpm dev
```

App runs on `http://localhost:3000`. Azure AD values in `.env.local` are optional.

---

## 📚 Documentation

### Getting Started
- **[Local Development](docs/LOCAL_DEVELOPMENT.md)** — Run local infrastructure and services
- **[API Docs](docs/api/USER_GUIDE.md)** — API architecture and usage
- **[App Setup](app/README.md)** — Frontend setup and structure
- **[Forking Guide](docs/FORKING_GUIDE.md)** — Rename and harden a new product fork
- **[Samples](docs/SAMPLES.md)** — What sample code exists and how to remove it
- **[Security](SECURITY.md)** — Reporting and secret incident response

### Architecture
- **[STRUCTURE.md](./docs/STRUCTURE.md)** — Complete project layout
- **[Tech Stack](#tech-stack)** — Technology decisions
- **[Dependency Constraints](./docs/DEPENDENCY_CONSTRAINTS.md)** — Intentionally pinned package majors and revisit criteria

### API Guides
- [Extending the API](docs/api/EXTENDING_THE_API.md)
- [Exception Handling](docs/api/EXCEPTION_HANDLING_GUIDE.md)
- [Validation Guide](docs/api/FLUENT_VALIDATION_GUIDE.md)
- [Result Pattern](docs/api/RESULT_PATTERN_GUIDE.md)
- [Configuration](docs/api/CONFIGURATION_GUIDE.md)

### Architecture Decision Records (ADR)
- [001 — Module + Feature Pattern](docs/adr/001-module-feature-pattern.md)
- [002 — Soft Delete with Restore](docs/adr/002-soft-delete-with-restore.md)
- [003 — Validation in Pipeline](docs/adr/003-validation-in-pipeline.md)
- [004 — EF Core over Repositories](docs/adr/004-ef-core-over-repositories.md)
- [005 — Audit Context Service](docs/adr/005-audit-context-service.md)
- [006 — Error Response Standardization](docs/adr/006-error-response-standardization.md)
- [007 — Unit of Work Pattern](docs/adr/007-unit-of-work-pattern.md)

---

## 🛠️ Tech Stack

| Layer | Technology |
|-------|-----------|
| **Backend Framework** | ASP.NET Core 10 (Minimal APIs) |
| **ORM** | Entity Framework Core + SQLite default, PostgreSQL/SQL Server optional |
| **CQRS** | MediatR |
| **Validation** | FluentValidation |
| **Auth (API)** | JWT + JWKS |
| **Logging** | Serilog (structured) |
| **Frontend Framework** | Next.js 16 (App Router) |
| **Language** | TypeScript |
| **Styling** | Tailwind CSS + shadcn/ui |
| **State Management** | Redux Toolkit |
| **Server State** | TanStack Query |
| **Auth (App)** | Better Auth |
| **RPC** | Hono (typed client/server) |
| **i18n** | next-intl (en / ko-KR / zh-TW) |
| **Package Manager** | pnpm |

---

## 📋 Features

### API Backend
✅ Minimal APIs with OpenAPI/Swagger  
✅ CQRS pattern with MediatR  
✅ Fluent validation pipeline  
✅ JWT authentication  
✅ Entity Framework Core with migrations  
✅ Soft delete archiving  
✅ Audit trail (created/updated tracking)  
✅ Structured logging with correlation IDs  
✅ Error standardization  
✅ Unit of Work pattern  
✅ Comprehensive tests (unit + integration)  

### Frontend App
✅ Next.js 16 with App Router  
✅ Server components + Client components  
✅ TypeScript strict mode  
✅ Tailwind CSS + shadcn/ui components  
✅ TanStack Query for server state  
✅ Redux Toolkit for client state  
✅ Better Auth integration  
✅ Hono RPC typed client  
✅ Internationalization (i18n)  
✅ Responsive design  

---

## 🔧 Development

### Verification

**API:**
```bash
cd api
dotnet test
```

**App:**
```bash
cd app
pnpm check
pnpm lint
pnpm build
```

CI runs these checks along with strict OpenSpec validation and a full-history Gitleaks scan.

### Code Style & Linting

**API:**
```bash
cd api
dotnet format  # Format code
```

**App:**
```bash
cd app
pnpm lint     # ESLint
pnpm lint:fix # ESLint autofix
```

---

## 📖 Project Structure

See **[STRUCTURE.md](./docs/STRUCTURE.md)** for complete documentation on:
- API project organization (`ForgeKit.Api/`, `ForgeKit.Api.Tests/`)
- Frontend structure (`app/`, `components/`, `features/`, `lib/`)
- Documentation (`docs/`, `openspec/`)
- Configuration files and their purposes

---

## 🤝 Contributing

1. Read **[CONTRIBUTING.md](./CONTRIBUTING.md)** for guidelines
2. Check **[openspec/AGENTS.md](./openspec/AGENTS.md)** for spec-driven development
3. Create feature branch: `git checkout -b feature/your-feature`
4. Commit with convention: `git commit -m "feat: description"`
5. Push and create a pull request

**Each section (API/App) has its own README for detailed setup instructions.**

---

## 📄 License

[Your License Here]

---

## 🙋 Support

- **API Questions?** → See [api/README.md](api/README.md) and [docs/api/USER_GUIDE.md](docs/api/USER_GUIDE.md)
- **App Questions?** → See [app/README.md](app/README.md)
- **Architecture?** → See [docs/STRUCTURE.md](docs/STRUCTURE.md) and ADRs
- **Specs & Changes?** → See [openspec/](openspec/) directory

---

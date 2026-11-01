# Agent Instructions

ForgeKit is a full-stack starter kit. Products are generated from it with
`dotnet new forgekit` and then diverge, so keep changes reusable across products rather than
tailored to the sample domain.

**Stack.** API: .NET 10 minimal API, EF Core (used directly, ADR-004), MediatR,
FluentValidation, Result pattern (ADR-006), JWT, Serilog, Scalar. App: Next.js 16 App Router,
TypeScript, Tailwind, shadcn/ui, Better Auth, Hono RPC, TanStack Query, next-intl. Database:
SQLite by default, PostgreSQL and SQL Server supported; the API and Better Auth share one
`Database__Provider` (root `.env`).

## The layer boundary in `api/`

- **`api/Anvil/`** is the shared layer. The template never renames it, so products pull base
  improvements with `git checkout upstream/main -- api/Anvil api/Anvil.Tests`. The build
  fails if it references the product project.
- **`api/ForgeKit.Api/`** is the product layer, renamed per product.

A file belongs in Anvil only if its transitive import closure contains no product type. If a
shared file seems to need one, invert the dependency through an Anvil-owned interface (see
`IDataSeeder`) instead of moving the file. Rationale: `docs/adr/008-shared-layer-boundary.md`.

## Building and testing

```bash
cd api && dotnet build && dotnet test
cd app && pnpm install && pnpm check && pnpm lint && pnpm test
pnpm verify                       # the full local gate, from the repository root
```

`pnpm test:e2e` (in `app/`) needs `pnpm auth.migration` and `pnpm build` first; it runs
against SQLite unless `Database__Provider` says otherwise (`docs/LOCAL_DEVELOPMENT.md`).
There is no component-level UI coverage: verify UI changes in the running app with the
`verify-forgekit` skill and attach its evidence.

## Conventions the checks cannot enforce

- **Add migrations for all three providers** when the EF model changes; never reuse one
  provider's migrations for another. CI catches a missing one, not a wrong one.
- **Product query filters must be named** — `HasQueryFilter("Name", ...)`. The soft-delete
  filter is `PlatformDbContext.SoftDeleteFilter`; an anonymous filter on a soft-delete entity
  fails at model build.
- **Conventional Commits** (`docs/api/COMMIT_CONVENTION.md`); branch off main and open a PR.
  Enable the hook that refuses commits on main: `git config core.hooksPath .githooks`.
- **Never commit local secrets.** `appsettings.Local.json` and `.env.local` are ignored and
  have `.example` counterparts.

## AI workflow

pstack is installed at project scope (`/pstack:poteto-mode <task>` is the entry point). When
the decisions are still open — a new product's domain, a new module, auth/tenant/permission
models, a public API shape — grill first: `/grill-with-docs` → `/to-spec` → pstack. Bug fixes,
refactors, and dependency bumps skip the grill. Details: `docs/AI_WORKFLOW.md`.

Issues and specs live in GitHub Issues (`docs/agents/issue-tracker.md`); `/to-spec` labels
them `ready-for-agent`. Domain docs are single-context: a root `GLOSSARY.md` created when the
first term is resolved, plus `docs/adr/` with three-digit numbering (`docs/agents/domain.md`).

## Where things are documented

| Topic | File |
|---|---|
| Project layout | `docs/STRUCTURE.md` |
| Generating a product, syncing base updates | `docs/FORKING_GUIDE.md` |
| Architecture decisions | `docs/adr/` |
| Platform (technical) terms | `docs/api/GLOSSARY.md` |
| Configuration and database providers | `docs/api/CONFIGURATION_GUIDE.md` |
| Structured logging | `docs/api/logging.md` |
| Local setup | `docs/LOCAL_DEVELOPMENT.md` |

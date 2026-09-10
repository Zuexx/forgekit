## Why

`openspec/specs/database-provider/spec.md` already requires SQLite as the zero-service default for
*both* the API and Better Auth, and requires them to move together when the provider changes. The
implementation does not do this: `app/lib/auth.config.ts` hardcodes `database = postgresDb`, no
SQLite adapter exists under `app/lib/db/`, and `compose.yaml` / `.env.local.example` assume Postgres
is required just to run the starter kit locally.

This is not cosmetic. `api/Anvil/Extensions/DatabaseProviderExtensions.cs`'s `AddConfiguredDbContext`
registers *both* `AppDbContext` and `BetterAuthDbContext` from the same `Database:Provider` switch —
default `Sqlite`. Left at real-world defaults, Better Auth (Next.js) writes users, sessions, and JWKs
into the Postgres container, while the API's `BetterAuthDbContext` — same schema, same entities —
resolves against a local SQLite file nobody wrote to. Two databases, one shape, silently disconnected.

Nothing observes this today: no API code queries `BetterAuthDbContext` outside migrations and tests
(`codegraph_explore` found 3 callers, all migration-tooling, ⚠️ no covering tests), and cross-service
auth verification is stateless (JWKS), not a DB read. It is a landmine, not a live bug — the same
shape as the `resolve-context.ts` cookie-name defect this repository's `authentication` spec was
written after: two configuration surfaces that must agree, agreeing only by accident.

## What Changes

- Add `app/lib/db/sqlite.ts`: a Kysely-based SQLite adapter, following the wrapper shape
  `mssql.ts` already documents (`{ db: kyselyInstance, type: "sqlite" as const }` — Better Auth's
  `database` option is untyped, so the wrong shape fails at runtime, not compile time).
- `app/lib/auth.config.ts`'s `database` selection becomes environment-driven instead of the
  hardcoded `postgresDb`, defaulting to the new SQLite adapter.
- The default SQLite adapter points at the **same physical file** `BetterAuthDbContext` resolves
  to by default, so local dev is one database, not two files that happen to share a schema.
- A single, documented mechanism for changing the provider that moves the API and Better Auth
  together — not two independent settings a fork can set inconsistently.
- `compose.yaml` and `.env.local.example` updated so SQLite is the true no-service default;
  Postgres/SQL Server remain the opt-in path for whoever wants them.
- Concurrent-write handling for the shared SQLite file: two separate processes (EF Core, Kysely)
  will open it, and SQLite's default journal mode serializes writers with a lock timeout that is
  easy to hit under two independent connection pools.

### Not included

- Renaming or restructuring the .NET-side `Database:Provider` mechanism itself — it is correct as
  specified; this change makes the frontend follow it, not the other way around.
- Migrating data out of an existing local Postgres setup — this changes the *default* for new
  clones and forks; an existing fork's `.env.local` keeps working unchanged.
- MSSQL end-to-end verification — `mssql.ts` already exists and is out of scope here beyond
  confirming it still fits the same selection mechanism.
- Anything about production deployment topology (managed Postgres, connection pooling at scale) —
  out of scope for a starter-kit default.

## Capabilities

### New Capabilities

_None._

### Modified Capabilities

- `database-provider`: the *Better Auth Uses Durable Local Storage* requirement's scenarios
  describe intent ("Local Better Auth uses SQLite", "Production Better Auth uses selected
  provider") that the implementation does not meet. The delta makes both scenarios concrete and
  testable — a shared file for the local case, a single provider-selection surface for the
  switched case — and adds a scenario for the failure mode this proposal exists to close: the two
  surfaces disagreeing.

## Impact

`codegraph_explore` findings (this repository, unlike forgekit-workflow, is indexed):

- **`BetterAuthDbContext`** (`api/Anvil/Data/Auth/BetterAuthDbContext.cs:6`) — 3 callers, all
  migration design-time factories; ⚠️ no covering tests found. Not exercised by application code
  today, which is exactly why the drift this proposal fixes has shipped unnoticed.
- **`auth`** (`app/lib/auth.config.ts:67`) — 6 callers: `app/app/api/auth/[[...all]]/route.ts`,
  `app/lib/rpc/session-middleware.ts`, plus i18n message files that match on the bare word "auth"
  and are not real dependents; ⚠️ no covering tests found. Changing `database`'s source touches
  every one of the real callers indirectly (they all go through this one exported instance).
- **The adapter shape is a runtime, not compile-time, contract.** `auth.config.ts`'s own comment
  says so: a `Pool` where a `{ db, type }` wrapper is expected fails at query time with
  `db.selectFrom is not a function`. `sqlite.ts` must match the wrapper shape, and this needs a
  runtime check (a query that succeeds), not just a type check, before this is called done.
- **Path resolution asymmetry.** `ResolveSqlitePath` in `DatabaseProviderExtensions.cs:88` resolves
  a relative SQLite path against `environment.ContentRootPath`, which for the API is
  `api/ForgeKit.Api/` — giving a default file at `api/ForgeKit.Api/data/forgekit.db`. The frontend
  runs from `app/`, a sibling directory; making both sides open the same file needs either a
  shared absolute/repo-relative path convention or one side to point at the other's resolved
  location. This is a design decision, not a mechanical fix — see `design.md`.
- **No existing cross-stack settings convention.** There is no root `.env`, and `app/.env.local.example`
  and `api/**/appsettings*.json` share no variable name today (checked by literal search — grep for
  `DATABASE_URL`, `Database__Provider`, `process.env.Database` across both trees). The
  "single mechanism that moves both sides together" this proposal asks for does not yet exist in
  any form and has to be designed, not just wired.
- **`compose.yaml`** currently declares only a `postgres` service (`forgekit-postgres`, port 5432).
  Nothing removes it — it remains what the opt-in Postgres path uses — but it stops being what a
  fresh clone needs to start.
- Test coverage for both changed symbols is presently zero (per `codegraph_explore`'s own flags),
  so this proposal's task list has to add coverage rather than lean on tests that do not exist.

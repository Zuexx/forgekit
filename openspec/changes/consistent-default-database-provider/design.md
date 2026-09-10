## Context

See `proposal.md` — Why. Confirmed with the user: the target shape is **one database service
instance per provider, shared by both processes, with the API's data and Better Auth's data kept
as two separate schemas within it** — not one merged schema, and not two disconnected databases
that happen to share table shapes (today's actual state for the default, unconfigured clone).

Current facts this design has to fit:

- `AddConfiguredDbContext<TContext>` (`api/Anvil/Extensions/DatabaseProviderExtensions.cs`)
  already resolves both `AppDbContext` and `BetterAuthDbContext` from one config key,
  `Database:Provider` (env override `Database__Provider`). That mechanism is correct and unchanged.
- Neither `AppDbContext` nor `BetterAuthDbContext` calls `HasDefaultSchema` today, so on
  Postgres/SQL Server both land in the default (`public`/`dbo`) schema, flat, alongside each other.
- `compose.yaml` already provides exactly one Postgres **service instance** — the "same instance"
  half of the requirement is already true for Postgres; what is missing is schema separation and a
  frontend that actually points at it by default.
- SQLite has no server process — "the same service instance" for SQLite is "the same file."
  EF Core's SQLite provider ignores `HasDefaultSchema` (documented behaviour, not an error), so
  calling it unconditionally is safe across all three providers.
- The frontend (`app/`) has no SQLite adapter and no shared-with-the-API settings surface at all.
- `.gitignore` ignores `api/ForgeKit.Api/data/` (the current, about-to-move default file location)
  and, unanchored, `*.db`/`*.db-shm`/`*.db-wal` everywhere. It also ignores `.env*` except
  `.env.*.example` — a plain `.env.example` at repo root does **not** match that exception
  (`.env.*.example` requires a literal `.` immediately after `.env.`, so `.env.example` is not
  matched by it) — a real trap for the new root env file's example.

## Goals / Non-Goals

**Goals:**

- One physical database per provider, addressed identically by the API and by Better Auth.
- The API's and Better Auth's tables stay in two distinguishable schemas within that one database.
- One setting changes the provider for both sides; no fork can set them inconsistently by editing
  only one file.
- The default (no configuration) path needs no external service.

**Non-Goals:**

- Merging `AppDbContext` and `BetterAuthDbContext` into one schema or one DbContext.
- A generic multi-tenant or multi-schema framework beyond these two, fixed schemas.
- Migrating an existing fork's already-populated local Postgres database — this changes the
  default for new clones; an existing `.env.local` pointing at a working Postgres instance keeps
  functioning, just outside the newly-added consistency checks until it opts in.

## Decisions

### Schema separation: `BetterAuthDbContext.OnModelCreating` calls `HasDefaultSchema("auth")`

Unconditional — Postgres and SQL Server create a real `auth` schema (`auth.user`, `auth.session`,
...); SQLite ignores the call and keeps one flat namespace, where the two DbContexts' table names
already do not collide. This is the one call that makes "two separate schemas" true everywhere
`HasDefaultSchema` means something, with zero provider branching. `AppDbContext` is left alone —
its tables stay in the provider's default schema, which is what "separate" is measured against.

This is a real migration for Postgres and SQL Server (tables move from `public`/`dbo` into
`auth`), not a no-op. Regenerated as part of this change; a fork with an existing local database
on those providers drops and recreates it, which is acceptable pre-launch starter-kit state, not
a production migration path.

### One shared database per provider: SQLite gets a single file at the repo root

`ConnectionStrings:Sqlite` in `appsettings.json` / `appsettings.Development.json` changes from
`Data Source=./data/forgekit.db` to `Data Source=../../data/forgekit.db`. `ResolveSqlitePath`
already combines a relative `DataSource` with `environment.ContentRootPath`
(`api/ForgeKit.Api/`), so this lands at `<repo root>/data/forgekit.db` with no code change to
`DatabaseProviderExtensions.cs` — only the connection string value moves.

`app/lib/db/sqlite.ts` resolves its default path the same way, from its own working directory:
`path.resolve(process.cwd(), "../data/forgekit.db")`, matching `postgres.ts`/`mssql.ts`'s existing
pattern of resolving paths from `process.cwd()`. Both sides create the directory if absent (the
API already does; the adapter gains the equivalent `fs.mkdirSync(dir, { recursive: true })`).
An absolute path in the connection string (rare, deployment-specific) is left as an explicit
override on either side — `ResolveSqlitePath` already special-cases a rooted path as-is.

Postgres and SQL Server need no topology change: `compose.yaml`'s one `postgres` service is
already the single instance both sides connect to once the frontend is wired to select it via
the same mechanism as the API (next decision).

### One setting for both sides: a root `.env` holding `Database__Provider`

A new file, `.env`, at the repository root — not `app/.env.local`, which stays for
Better-Auth-specific and per-app secrets. It carries exactly one line:
`Database__Provider=Sqlite`. Chosen over inventing a new variable name so there is only ever one
literal key to keep in sync in anyone's head, and it is the name .NET's configuration system
already binds via the standard double-underscore section-path convention — nothing new to
document on that side.

- **Next.js side**: `app/lib/db/sqlite.ts` (and the selection point in `auth.config.ts`) loads it
  with the same explicit-path `dotenv` pattern `postgres.ts`/`mssql.ts` already use —
  `config({ path: resolve(process.cwd(), "../.env") })` — before reading
  `process.env.Database__Provider`.
- **.NET side**: `Program.cs` gains one line, `DotNetEnv.Env.Load(Path.Combine(builder.Environment.ContentRootPath, "..", ".env"))`,
  before the configuration is read. `DotNetEnv` sets process environment variables, and ASP.NET
  Core's environment-variable configuration provider already binds `Database__Provider` onto
  `Database:Provider` — this is additive to the existing `Database:Provider` mechanism, not a
  replacement for it. A file that does not exist is tolerated (`Env.Load` overload configured to
  not throw), so a deployment that sets `Database__Provider` through real environment variables
  (a container, a platform's config) needs no `.env` file at all and is unaffected.

**Rejected alternative — a helper script that writes both sides' settings.** Considered and
rejected: it synchronises at the moment it is run, but nothing stops a later hand-edit to one
side from drifting again — which is the exact failure this proposal exists to close. A single
file both runtimes read structurally cannot drift the way two independently-written files can.

### Concurrent local writes: WAL journal mode and an explicit busy timeout

Two independent connection pools (EF Core's, `better-sqlite3`'s) opening one file need
`PRAGMA journal_mode=WAL` (allows one writer concurrent with readers, rather than
whole-database-file locking) and a non-zero `PRAGMA busy_timeout` (so a writer waits for a
released lock instead of failing immediately with `SQLITE_BUSY`). Both are set on every
connection open on both sides — WAL mode is a property of the file after the first successful
set, but setting it every time is cheap and does not depend on which side opens the file first.

### `sqlite.ts` adapter shape

`better-sqlite3` (synchronous, the standard driver for Kysely's `SqliteDialect`) wrapped exactly
like `mssql.ts`: `export const database = { db: sqliteKyselyInstance, type: "sqlite" as const }`.
The selection point in `auth.config.ts` picks `sqlite.ts`, `postgres.ts`, or `mssql.ts`'s export
by reading `Database__Provider`, normalised the same way `NormalizeProvider` does on the .NET
side (case-insensitive, `sqlite|postgres|sqlserver`), defaulting to `sqlite` when unset — matching
the API's own `DefaultProvider = "Sqlite"`.

## Risks / Trade-offs

- **`.env` naming collides with the gitignore trap noted in Context.** The new file's example
  (`.env.example` at repo root) is not matched by the existing `!.env.*.example` exception and
  would be silently ignored by `git add`. Fixed as a task: either add a matching exception or
  name the example `.env.root.example` to fit the existing pattern — the task picks whichever
  keeps the naming least surprising and verifies with `git check-ignore`.
- **`DotNetEnv` is a new NuGet dependency** for one call in `Program.cs`. Small, single-purpose,
  widely used; accepted for the alternative it removes (a helper script that can drift).
- **Postgres/SQL Server schema move is a breaking migration** for anyone with an existing local
  database on those providers. Acceptable pre-launch; called out in the migration plan below and
  in the PR description, not silently absorbed.
- **WAL mode leaves extra `-wal`/`-shm` files beside the `.db` file.** Already covered by the
  existing unanchored `*.db-shm` / `*.db-wal` gitignore entries; verified, not assumed.
- **A fork that already runs Postgres via a hand-set `DATABASE_URL` and never adopts the root
  `.env`** keeps working (nothing removes `postgres.ts` or `DATABASE_URL`), but sits outside the
  new consistency guarantee until it adopts `Database__Provider`. Documented, not silently
  papered over — the spec's new scenario describes the guaranteed path, not a retroactive claim
  about every possible existing fork.

## Migration Plan

1. Add `HasDefaultSchema("auth")`, regenerate Postgres, SQL Server, **and SQLite** migrations —
   verified during implementation that SQLite also needs a migration (`has-pending-model-changes`
   flags the model annotation regardless of provider), but applying it is a harmless no-op:
   table names stay bare, confirmed against a throwaway file.
2. Move the SQLite connection string to the repo-root path; update `.gitignore`.
3. Add `app/lib/db/sqlite.ts`, wire `Database__Provider`-based selection into `auth.config.ts`,
   add `better-sqlite3` to `app/package.json`.
4. Add the root `.env` / `.env.example`, `DotNetEnv.Env.Load(...)` in `Program.cs`.
5. Update `compose.yaml` comments / `README.md` / `docs/FORKING_GUIDE.md` so SQLite reads as the
   true zero-service default and switching providers is documented as "edit one file."
6. Regenerate the template's `dotnet new forgekit` smoke test to confirm a generated product still
   builds, migrates, and starts with the new default.

**Rollback:** revert the connection-string and `HasDefaultSchema` changes; the root `.env`
mechanism is additive (an absent file changes nothing), so removing it is a clean revert with no
migration to run backward.

## Open Questions

None — the two decisions that would have changed scope (shared-instance-with-separate-schemas,
the settings mechanism) are resolved above.

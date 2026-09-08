All blocking decisions are settled in `design.md` — Decisions (schema separation via
`HasDefaultSchema("auth")`, shared SQLite file at the repo root, the root `.env` /
`Database__Provider` mechanism over a helper script, WAL + busy-timeout for concurrent local
writes, the `sqlite.ts` adapter shape). SQLite stays the default provider on both sides — nothing
here changes `DefaultProvider = "Sqlite"` on the API or the frontend's fallback. No open
questions.

## 1. Schema separation

- [x] 1.1 Add `modelBuilder.HasDefaultSchema("auth")` to `BetterAuthDbContext.OnModelCreating`;
  regenerate the Postgres and SQL Server migrations so `account`/`jwks`/`session`/`user`/
  `verification` move into the `auth` schema. Corrected during implementation, verified rather
  than assumed: the SQLite provider *does* need its own `AddAuthSchema` migration too — the
  model annotation is a diff `has-pending-model-changes` detects regardless of provider, so
  skipping it would leave that check permanently red — but applying it is a harmless no-op:
  `RenameTable(..., newSchema: "auth")` executes against a live SQLite file with the table
  names left bare (confirmed by applying it to a throwaway file and inspecting `.tables`).
  _Done when:_ `dotnet ef database update` against a fresh Postgres and a fresh SQL Server
  instance creates every Better Auth table under `auth.*`; `AppDbContext`'s tables are unchanged;
  applying the SQLite `AddAuthSchema` migration to a fresh file leaves table names bare (no
  `auth.` prefix, no error); the full test suite passes for all three providers.
  **Verified live, not just via model tests:** `forgekit-postgres` (already running) → 5 tables
  under `auth.*` via `information_schema.tables`. A throwaway `mssql/server:2022-latest`
  container → 5 tables under `auth.*` via `sys.tables`/`sys.schemas`, then removed. SQLite → a
  throwaway file with both migrations applied lists bare table names via `.tables`, including
  both `AppDbContext`'s tables and Better Auth's in the one file.

## 2. Shared SQLite file at the repo root

- [ ] 2.1 Change `ConnectionStrings:Sqlite` in `appsettings.json` and
  `appsettings.Development.json` from `Data Source=./data/forgekit.db` to
  `Data Source=../../data/forgekit.db`, so `ResolveSqlitePath` lands the default file at
  `<repo root>/data/forgekit.db`. Update `.gitignore`: drop the now-stale
  `api/ForgeKit.Api/data/` entry, add `/data/` at the repo root (the unanchored `*.db`/
  `*.db-shm`/`*.db-wal` entries already cover the files themselves).
  _Done when:_ running the API with no other configuration creates
  `<repo root>/data/forgekit.db`, not the old path; `git status` shows nothing new under the
  new location; the existing test suite (which sets its own connection strings) is unaffected.

## 3. One setting for both sides

- [ ] 3.1 Add a root `.env.example` containing `Database__Provider=Sqlite` with a comment
  explaining it is the one setting shared by the API and Better Auth; verify with
  `git check-ignore -v .env.example` that it is tracked (the existing `!.env.*.example`
  exception does not match a plain `.env.example`, so this may need either the exception
  widened or the example named to fit it — pick whichever leaves the least surprising
  filename and confirm the check passes).
  _Done when:_ `git check-ignore -v .env.example` exits 1 (not ignored) and `git status` shows
  the file staged when added.
- [ ] 3.2 Add the `DotNetEnv` package to `ForgeKit.Api.csproj` and load the repo-root `.env` in
  `Program.cs` before configuration is used, tolerating a missing file.
  _Done when:_ a repo-root `.env` containing `Database__Provider=Postgres` causes
  `IConfiguration["Database:Provider"]` to read `Postgres` with no other change; deleting the
  file falls back to the existing `Sqlite` default; a real environment variable
  `Database__Provider` set outside any `.env` file still works unchanged (platform/container
  deployments are not required to have the file).

## 4. Frontend SQLite adapter and provider selection

- [ ] 4.1 Add `better-sqlite3` to `app/package.json`. Add `app/lib/db/sqlite.ts`: a Kysely
  `SqliteDialect` over `better-sqlite3`, wrapped as `{ db, type: "sqlite" as const }` matching
  `mssql.ts`'s shape, defaulting its file path to
  `path.resolve(process.cwd(), "../data/forgekit.db")` and creating the directory if absent.
  _Done when:_ a real Kysely query (not just a type check) through the new adapter against a
  throwaway SQLite file succeeds — the adapter-shape mismatch this kit has already been bitten
  by fails at query time, not compile time, so this has to be exercised, not just built.
- [ ] 4.2 In `app/lib/auth.config.ts`, replace the hardcoded `export const database = postgresDb`
  with a selection driven by `Database__Provider` (loaded via the same explicit repo-root
  `dotenv` pattern `postgres.ts`/`mssql.ts` already use), normalised the same way the API's
  `NormalizeProvider` is (`sqlite|postgres|sqlserver`, case-insensitive) and defaulting to
  `sqlite` when the setting is absent.
  _Done when:_ with no root `.env` present, `auth` uses the SQLite adapter from 4.1; with
  `Database__Provider=Postgres` set, it uses `postgresDb` unchanged from today's behaviour; the
  existing `pnpm test` vitest suite still passes.

## 5. Concurrent local writes

- [ ] 5.1 Set `PRAGMA journal_mode=WAL` and a non-zero `PRAGMA busy_timeout` on every SQLite
  connection open, on both the API side (an EF Core connection-interceptor or equivalent
  connection-opened hook) and in `sqlite.ts` (`better-sqlite3`'s `.pragma()`).
  _Done when:_ a test that opens a .NET EF Core connection and a `better-sqlite3` connection to
  the same file and writes from both at roughly the same time completes without a
  `SQLITE_BUSY`/"database is locked" error, run and observed to fail before this task and pass
  after.

## 6. Documentation

- [ ] 6.1 Update `compose.yaml`'s comments, `README.md`, and `docs/FORKING_GUIDE.md` (and any
  ADR that currently states or implies Postgres is required to start the kit) to describe
  SQLite as the zero-service default and the root `.env`'s `Database__Provider` as the single
  switch that moves both the API and Better Auth together.
  _Done when:_ a search for the previous "Postgres required" framing finds nothing live; the
  updated docs name the root `.env` file and its one key, and state that editing only one of
  `app/.env.local` or `appsettings.json` no longer has any effect on the provider.

## 7. End-to-end verification through the template

- [ ] 7.1 Generate a product with `dotnet new forgekit`, confirm it builds and migrates on the
  default (SQLite) and at least one of Postgres/SQL Server with the new `auth` schema, then
  demonstrate — not assume — the spec's "share one SQLite database" scenario: a row written
  through a Better Auth sign-up (Next.js, `pnpm dev`) is readable by an API-side query against
  `BetterAuthDbContext` without restarting either process.
  _Done when:_ the generated product builds with 0 errors, its existing test suite passes
  (record the count), and the cross-process read-back above is observed in this session, not
  inferred from the two sides "both using SQLite."

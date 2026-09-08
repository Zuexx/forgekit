# Consistent Default Database Provider Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** The API and Better Auth (Next.js) share one database service instance per provider — SQLite by default, one file, no external service — with the API's data and Better Auth's data kept in two separate schemas, and one setting moves both sides together.

**Architecture:** `BetterAuthDbContext` gains `HasDefaultSchema("auth")`, regenerating the Postgres and SQL Server migrations; SQLite ignores the call. The API's default SQLite connection string moves to a repo-root file both processes resolve to independently. A new root `.env` carries the one shared setting, `Database__Provider` — .NET picks it up via `DotNetEnv` loaded before the host builder reads configuration; Next.js picks it up via the same explicit-`dotenv` pattern `postgres.ts`/`mssql.ts` already use. A new `app/lib/db/sqlite.ts` adapter, lazily constructed like `mssql.ts`'s pool, lets `auth.config.ts` select it by default.

**Tech Stack:** .NET 10 / EF Core 10 (Npgsql, SqlServer, Sqlite providers), `DotNetEnv`; Next.js / Kysely / `better-sqlite3`; `podman` + `compose.yaml` for the local Postgres instance.

**Spec:** `openspec/changes/consistent-default-database-provider/` — `proposal.md`, `specs/database-provider/spec.md`, `design.md`, `tasks.md`. Read both; this plan expands `tasks.md` into steps.

## Global Constraints

- Central Package Management is active in `api/` (`Directory.Packages.props`) — a new package version goes there, never a `Version=` attribute on a `<PackageReference>` in a `.csproj`.
- `ConfigurationManager` (`builder.Configuration` in `Program.cs`) snapshots environment variables when `WebApplication.CreateBuilder(args)` runs. `DotNetEnv.Env.Load()` **must** execute before that call, or the loaded values are invisible to configuration — verified as a task step, not assumed.
- `EF Core`'s SQLite provider does not error on `HasDefaultSchema` — it is accepted and stored on the model but not honoured by the SQLite SQL generator. Model-level assertions (`Model.GetDefaultSchema()`) and SQL-generation assertions (whether table names come out schema-qualified) are different checks; this plan's tests do the one that matches each provider's actual behaviour, verified rather than assumed for SQLite.
- Kysely's `SqliteDialect` accepts `database` as an instance **or** a factory function; `mssql.ts`'s pool (`tarn`) and `postgres.ts`'s `pg.Pool` are both lazy — no live connection at import time. `sqlite.ts` uses the factory form so importing it does not eagerly open/create the file, matching the other two adapters' behaviour.
- Commits follow Conventional Commits, lowercase imperative subject.
- Attribution footer on every commit:
  ```
  Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
  ```

## OpenSpec Coverage

Change: `openspec/changes/consistent-default-database-provider`

| Task ids | Plan task |
| --- | --- |
| 1.1 | Task 1 — `auth` schema separation |
| 2.1 | Task 2 — shared SQLite file at the repo root |
| 3.1, 3.2 | Task 3 — root `.env` / `Database__Provider` |
| 4.1, 4.2 | Task 4 — frontend SQLite adapter and provider selection |
| 5.1 | Task 5 — WAL + busy timeout for concurrent local writes |
| 6.1 | Task 6 — documentation |
| 7.1 | Task 7 — end-to-end verification through the template |

---

## Task 1: `auth` schema separation

Delivers OpenSpec task **1.1**. Covers spec scenarios: *Sharing an instance does not merge the schemas*, and the schema half of *Production Better Auth uses selected provider*.

**Files:**
- Modify: `api/Anvil/Data/Auth/BetterAuthDbContext.cs`
- Create: `api/ForgeKit.Api.Tests/Data/SchemaSeparationTests.cs`
- Create: `api/ForgeKit.Api.Migrations.Postgres/Migrations/*_AddAuthSchema.cs` (generated)
- Create: `api/ForgeKit.Api.Migrations.SqlServer/Migrations/*_AddAuthSchema.cs` (generated)

**Interfaces:**
- Consumes: nothing new.
- Produces: `BetterAuthDbContext`'s model now carries a default schema of `"auth"`. `AppDbContext`'s model is unaffected (no default schema set on it — it stays whatever the provider's own default is, `public` for Postgres, `dbo` for SQL Server, none for SQLite).

- [ ] **Step 1: Add the schema call**

In `api/Anvil/Data/Auth/BetterAuthDbContext.cs`, add `modelBuilder.HasDefaultSchema("auth");` as the first line inside `OnModelCreating`, after `base.OnModelCreating(modelBuilder);`:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    // Keeps Better Auth's tables out of the same namespace as the product's tables when
    // both share one database instance (Postgres, SQL Server). EF Core's SQLite provider
    // accepts this call but does not honour it — SQLite has no schema concept, and the
    // two DbContexts' table names do not collide, so one flat file is still "separate".
    modelBuilder.HasDefaultSchema("auth");

    modelBuilder.Entity<Account>()
        .HasOne(a => a.User)
        .WithMany(u => u.Accounts)
        .HasForeignKey(a => a.UserId)
        .OnDelete(DeleteBehavior.Cascade);

    modelBuilder.Entity<Session>()
        .HasOne(s => s.User)
        .WithMany(u => u.Sessions)
        .HasForeignKey(s => s.UserId)
        .OnDelete(DeleteBehavior.Cascade);
}
```

- [ ] **Step 2: Write the model-level regression test (no live database needed)**

Create `api/ForgeKit.Api.Tests/Data/SchemaSeparationTests.cs`. This uses the same
`IDesignTimeDbContextFactory` classes `MigrationProjectTests.cs` already uses — building the EF
Core model is in-memory and needs no live connection:

```csharp
using Microsoft.EntityFrameworkCore;
using Shouldly;
using PostgresMigrations = ForgeKit.Api.Migrations.Postgres;
using SqlServerMigrations = ForgeKit.Api.Migrations.SqlServer;
using SqliteMigrations = ForgeKit.Api.Migrations.Sqlite;

namespace ForgeKit.Api.Tests.Data;

public sealed class SchemaSeparationTests
{
    [Fact]
    public void Postgres_BetterAuthDbContext_UsesAuthSchema()
    {
        using var context = new PostgresMigrations.BetterAuthDbContextFactory().CreateDbContext([]);
        context.Model.GetDefaultSchema().ShouldBe("auth");
    }

    [Fact]
    public void Postgres_AppDbContext_DoesNotUseAuthSchema()
    {
        using var context = new PostgresMigrations.AppDbContextFactory().CreateDbContext([]);
        context.Model.GetDefaultSchema().ShouldNotBe("auth");
    }

    [Fact]
    public void SqlServer_BetterAuthDbContext_UsesAuthSchema()
    {
        using var context = new SqlServerMigrations.BetterAuthDbContextFactory().CreateDbContext([]);
        context.Model.GetDefaultSchema().ShouldBe("auth");
    }

    [Fact]
    public void SqlServer_AppDbContext_DoesNotUseAuthSchema()
    {
        using var context = new SqlServerMigrations.AppDbContextFactory().CreateDbContext([]);
        context.Model.GetDefaultSchema().ShouldNotBe("auth");
    }

    [Fact]
    public void Sqlite_BetterAuthDbContext_GeneratesNoSchemaQualifiedSql()
    {
        // The provider accepts HasDefaultSchema but SQLite has no schema concept, so the
        // generated CREATE TABLE statement must not be schema-qualified. This is the
        // behavioural check; the model-level annotation may still be present (that part is
        // not asserted here — it is a provider detail, not a contract this kit depends on).
        using var context = new SqliteMigrations.BetterAuthDbContextFactory().CreateDbContext([]);
        var sql = context.Database.GenerateCreateScript();
        sql.ShouldNotContain("\"auth\".");
    }
}
```

- [ ] **Step 3: Run it and confirm it fails (no schema call yet is wrong to assume — verify by checking out Step 1's change is really needed)**

Run: `dotnet test api/ForgeKit.Api.Tests --filter SchemaSeparationTests`
Expected: if Step 1 is already applied, this passes; if you are following the plan in strict
order it will already be green since Step 1 came first — the point of this step is to have
actually run it, not to assume.

- [ ] **Step 4: Generate the Postgres migration**

Run:
```bash
dotnet ef migrations add AddAuthSchema \
  --project api/ForgeKit.Api.Migrations.Postgres \
  --startup-project api/ForgeKit.Api.Migrations.Postgres \
  --context BetterAuthDbContext
```
Expected: a new migration file under `api/ForgeKit.Api.Migrations.Postgres/Migrations/`
containing `migrationBuilder.EnsureSchema(name: "auth");` and `RenameTable`/`CreateTable` calls
moving `account`, `jwks`, `session`, `user`, `verification` into `schema: "auth"`.

- [ ] **Step 5: Generate the SQL Server migration**

Run:
```bash
dotnet ef migrations add AddAuthSchema \
  --project api/ForgeKit.Api.Migrations.SqlServer \
  --startup-project api/ForgeKit.Api.Migrations.SqlServer \
  --context BetterAuthDbContext
```
Expected: same shape as Step 4, under `api/ForgeKit.Api.Migrations.SqlServer/Migrations/`.

- [ ] **Step 6: Confirm the SQLite migration set is unaffected**

Run:
```bash
dotnet ef migrations has-pending-model-changes \
  --project api/ForgeKit.Api.Migrations.Sqlite \
  --startup-project api/ForgeKit.Api.Migrations.Sqlite \
  --context BetterAuthDbContext
```
Expected: no pending changes reported (exit 0, no "Reminder" message about pending changes) —
`HasDefaultSchema` produced no diff for the SQLite provider, so no new migration file should
exist under `api/ForgeKit.Api.Migrations.Sqlite/Migrations/`. Confirm with
`git status api/ForgeKit.Api.Migrations.Sqlite` showing nothing new.

- [ ] **Step 7: Apply the Postgres migration to the running container and verify live**

The `forgekit-postgres` container from `compose.yaml` is already up. Run:
```bash
dotnet ef database update \
  --project api/ForgeKit.Api.Migrations.Postgres \
  --startup-project api/ForgeKit.Api.Migrations.Postgres \
  --context BetterAuthDbContext \
  --connection "Host=localhost;Database=forgekit"

podman exec forgekit-postgres psql -U postgres -d forgekit -c \
  "select table_schema, table_name from information_schema.tables where table_schema = 'auth' order by table_name;"
```
Expected: five rows — `auth.account`, `auth.jwks`, `auth.session`, `auth.user`,
`auth.verification`.

- [ ] **Step 8: SQL Server — best-effort live verification**

SQL Server needs a container this machine does not have running yet, and is heavier than
Postgres. Attempt it:
```bash
podman run -d --name forgekit-mssql-verify -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD='P@ssw0rd_verify1' \
  -p 1434:1433 mcr.microsoft.com/mssql/server:2022-latest
```
If it starts and becomes healthy within a few minutes, apply and verify the same way as Step 7
(`dotnet ef database update` against it, then query `sys.schemas`/`sys.tables` for the `auth`
schema), then `podman rm -f forgekit-mssql-verify`. **If it does not come up cleanly in
reasonable time** (image pull too slow, resource pressure), stop it, remove the container, and
record in the commit/report that SQL Server was verified through Steps 2 and 5 only
(model-level test + generated migration content) — consistent with what `proposal.md` scoped as
acceptable, not a silent gap.

- [ ] **Step 9: Run the full backend test suite**

Run: `dotnet test`
Expected: all tests pass, including the new `SchemaSeparationTests` and the existing
`MigrationProjectTests`.

- [ ] **Step 10: Commit**

```bash
git add api/Anvil/Data/Auth/BetterAuthDbContext.cs \
        api/ForgeKit.Api.Tests/Data/SchemaSeparationTests.cs \
        api/ForgeKit.Api.Migrations.Postgres/Migrations \
        api/ForgeKit.Api.Migrations.SqlServer/Migrations
git commit -m "feat(api): separate Better Auth into its own database schema

BetterAuthDbContext now sets HasDefaultSchema(\"auth\"), so a shared
Postgres or SQL Server instance keeps Better Auth's tables out of the
product's namespace. SQLite ignores the call — one file, distinct table
names, no schema support to separate with.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
```

## Task 2: Shared SQLite file at the repo root

Delivers OpenSpec task **2.1**. Covers the SQLite half of spec scenario *The API and Better Auth share one SQLite database*.

**Files:**
- Modify: `api/ForgeKit.Api/appsettings.json`, `api/ForgeKit.Api/appsettings.Development.json`
- Modify: `.gitignore`
- Modify: `api/ForgeKit.Api.Tests/Extensions/DatabaseProviderExtensionsTests.cs`

**Interfaces:**
- Consumes: `DatabaseProviderExtensions.ResolveSqlitePath` (unchanged — only the connection
  string value it is fed changes).
- Produces: nothing new; the default SQLite file moves from
  `api/ForgeKit.Api/data/forgekit.db` to `<repo root>/data/forgekit.db`.

- [ ] **Step 1: Write the path-arithmetic regression test first**

Add to `api/ForgeKit.Api.Tests/Extensions/DatabaseProviderExtensionsTests.cs`:

```csharp
[Fact]
public void ResolveSqlitePath_DefaultConnectionString_ResolvesToRepoRootDataDirectory()
{
    // Mirrors the real layout: content root is api/ForgeKit.Api, two levels up is the repo
    // root. This is the exact connection string appsettings.json ships — a regression here
    // means the API and the frontend stop agreeing on where the shared file lives.
    var repoRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    var contentRoot = Path.Combine(repoRoot, "api", "ForgeKit.Api");
    Directory.CreateDirectory(contentRoot);
    var settings = new DatabaseProviderSettings("Sqlite", "Data Source=../../data/forgekit.db");

    var resolved = settings.ResolveSqlitePath(contentRoot);

    resolved.ConnectionString.ShouldContain(Path.Combine(repoRoot, "data", "forgekit.db"));
}
```

- [ ] **Step 2: Run it and confirm it fails**

Run: `dotnet test api/ForgeKit.Api.Tests --filter ResolveSqlitePath_DefaultConnectionString_ResolvesToRepoRootDataDirectory`
Expected: FAIL — the connection string in `appsettings.json` has not changed yet, but this test
asserts against the *new* value (`../../data/forgekit.db`), so this specific assertion should
already read correctly once Step 1 lands. If it fails at this point, it means the test's own
math is wrong — check before moving on rather than assuming Step 3 will fix it.

- [ ] **Step 3: Move the connection string**

In both `api/ForgeKit.Api/appsettings.json` and `api/ForgeKit.Api/appsettings.Development.json`,
change:
```
"Sqlite": "Data Source=./data/forgekit.db",
```
to:
```
"Sqlite": "Data Source=../../data/forgekit.db",
```

- [ ] **Step 4: Update `.gitignore`**

Replace:
```
api/ForgeKit.Api/data/
```
with:
```
/data/
```
(The unanchored `*.db`/`*.db-shm`/`*.db-wal` entries a few lines below already cover the files
themselves at any path; this just retires the now-stale directory-specific entry and ignores
the new one.)

- [ ] **Step 5: Run the test again and confirm it passes**

Run: `dotnet test api/ForgeKit.Api.Tests --filter ResolveSqlitePath_DefaultConnectionString_ResolvesToRepoRootDataDirectory`
Expected: PASS.

- [ ] **Step 6: Smoke-test the real path with a brief run**

```bash
rm -rf data api/ForgeKit.Api/data   # clean slate for the check
(cd api/ForgeKit.Api && timeout 15 dotnet run --no-launch-profile >/tmp/forgekit-api-run.log 2>&1 &)
sleep 8
ls -la data/forgekit.db && echo "FOUND at repo root"
[ -e api/ForgeKit.Api/data/forgekit.db ] && echo "UNEXPECTED: still at old path"
pkill -f "ForgeKit.Api.dll" 2>/dev/null || true
```
Expected: `data/forgekit.db` exists at the repo root; nothing appears at the old
`api/ForgeKit.Api/data/` path.

- [ ] **Step 7: Run the full backend test suite**

Run: `dotnet test`
Expected: all tests pass.

- [ ] **Step 8: Commit**

```bash
git add api/ForgeKit.Api/appsettings.json api/ForgeKit.Api/appsettings.Development.json \
        .gitignore api/ForgeKit.Api.Tests/Extensions/DatabaseProviderExtensionsTests.cs
git commit -m "feat(api): default the SQLite database to the repo root

Moves the default SQLite connection string from api/ForgeKit.Api/data/
to <repo root>/data/, so the frontend's adapter (added in a later task)
resolves to the same file without extra configuration.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
```

## Task 3: Root `.env` / `Database__Provider`

Delivers OpenSpec tasks **3.1** and **3.2**. Covers spec scenario *One setting selects the provider for both sides*.

**Files:**
- Create: `.env.example` (repo root)
- Modify: `.gitignore`
- Modify: `api/Directory.Packages.props`, `api/ForgeKit.Api/ForgeKit.Api.csproj`
- Modify: `api/ForgeKit.Api/Program.cs`

**Interfaces:**
- Consumes: `DotNetEnv.Env.TraversePath().Load()` — walks up from the current directory looking
  for a `.env` file, tolerating none found. Chosen over a hardcoded `../..` path so this does
  not depend on `dotnet run`'s working directory convention matching the SQLite path's assumed
  layout.
- Produces: nothing new exported; `Environment.GetEnvironmentVariable("Database__Provider")`
  becomes visible to ASP.NET Core's configuration system before `CreateBuilder` captures it.

- [ ] **Step 1: Add the root `.env.example`**

Create `.env.example` at the repository root:

```
# The one setting shared by the API and Better Auth (Next.js) — copy this file to `.env` and
# edit there, not in app/.env.local or api's appsettings.json. Both processes read this file;
# editing only one side no longer changes which database either one uses.
#
# Values: Sqlite (default, no external service needed), Postgres, SqlServer
Database__Provider=Sqlite
```

- [ ] **Step 2: Fix the gitignore trap and verify**

`.gitignore` already has `.env*` / `!.env.*.example`, and `!.env.*.example` does not match a
plain `.env.example` (it requires a literal `.` immediately after `.env.`). Add a second
exception directly below the existing one:

```
.env*
!.env.*.example
!.env.example
```

Verify:
```bash
git check-ignore -v .env.example; echo "exit=$?"
```
Expected: no output, `exit=1` — the file is not ignored.

- [ ] **Step 3: Add the `DotNetEnv` package**

In `api/Directory.Packages.props`, add (alphabetical, matching the existing list's order):
```xml
<PackageVersion Include="DotNetEnv" Version="3.1.1" />
```
In `api/ForgeKit.Api/ForgeKit.Api.csproj`, add to the `<ItemGroup>` of package references (no
`Version` attribute — Central Package Management supplies it):
```xml
<PackageReference Include="DotNetEnv" />
```
Run: `dotnet restore api/ForgeKit.Api.sln` (or the solution file this repo uses) and confirm it
resolves.

- [ ] **Step 4: Load the root `.env` before the host builder reads configuration**

In `api/ForgeKit.Api/Program.cs`, add as the very first statement — **before**
`var builder = WebApplication.CreateBuilder(args);` — because `ConfigurationManager` snapshots
environment variables at that call, and anything set after is invisible to it without an
explicit reload:

```csharp
using DotNetEnv;

// Must run before CreateBuilder(args): ASP.NET Core's environment-variable configuration
// provider reads the process environment when the host builder is constructed, not lazily.
// TraversePath() walks up from the current directory looking for a `.env` file rather than
// assuming a fixed depth, so this works whether the process starts from api/ForgeKit.Api (dotnet
// run) or elsewhere. A deployment with no `.env` file (real environment variables set by a
// container or platform instead) is unaffected — a missing file is not an error here.
Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
```

- [ ] **Step 5: Confirm a missing `.env` does not throw**

Run: `dotnet build api/ForgeKit.Api` then, from a directory with no `.env` anywhere above it
(e.g. `/tmp`), run the built binary briefly:
```bash
cd /tmp && timeout 5 dotnet /Users/zuexx/Documents/labs/forgekit-family/forgekit/api/ForgeKit.Api/bin/Debug/net10.0/ForgeKit.Api.dll >/tmp/no-env-run.log 2>&1
grep -i 'exception\|Env.Load' /tmp/no-env-run.log && echo "UNEXPECTED FAILURE" || echo "no crash from missing .env"
```
Expected: `no crash from missing .env` (the process may still exit for other reasons in 5s, but
not because of `Env.TraversePath().Load()`).

- [ ] **Step 6: Confirm the root `.env` actually changes the resolved provider**

```bash
cp .env.example .env
sed -i '' 's/Sqlite/Postgres/' .env
```
Add a one-line startup log (kept permanently — it is useful observability, not just a test aid)
right after the two `AddConfiguredDbContext` registrations in `Program.cs`:
```csharp
Log.Information("Database provider: {Provider}",
    Anvil.Extensions.DatabaseProviderExtensions.GetDatabaseProviderSettings(builder.Configuration).Provider);
```
Run: `(cd api/ForgeKit.Api && timeout 8 dotnet run --no-launch-profile 2>&1 | tee /tmp/provider-check.log &); sleep 6`
Expected: `/tmp/provider-check.log` contains `Database provider: Postgres`. Then:
```bash
rm .env
(cd api/ForgeKit.Api && timeout 8 dotnet run --no-launch-profile 2>&1 | tee /tmp/provider-check-2.log &); sleep 6
```
Expected: `/tmp/provider-check-2.log` contains `Database provider: Sqlite` — confirming both the
override and the fallback.

- [ ] **Step 7: Run the full backend test suite**

Run: `dotnet test`
Expected: all tests pass (the new log line and `Env.Load()` call do not change any DI wiring).

- [ ] **Step 8: Commit**

```bash
git add .env.example .gitignore api/Directory.Packages.props \
        api/ForgeKit.Api/ForgeKit.Api.csproj api/ForgeKit.Api/Program.cs
git commit -m "feat(api): read the shared provider setting from a root .env

Env.TraversePath().Load() runs before CreateBuilder(args) so
Database__Provider set in a repository-root .env reaches configuration
the same way a real environment variable would. A missing .env changes
nothing — deployments that set real environment variables are
unaffected. Logs the resolved provider at startup.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
```

## Task 4: Frontend SQLite adapter and provider selection

Delivers OpenSpec tasks **4.1** and **4.2**. Covers spec scenarios: *Local Better Auth uses SQLite*, the frontend half of *One setting selects the provider for both sides* and *The API and Better Auth share one SQLite database*.

**Files:**
- Modify: `app/package.json`
- Create: `app/lib/db/sqlite.ts`
- Modify: `app/lib/auth.config.ts`
- Modify: `app/lib/auth.config.test.ts`

**Interfaces:**
- Consumes: `Database__Provider` (from the root `.env`, same key the API reads).
- Produces: `app/lib/db/sqlite.ts` exports `db: Kysely<DB>` (unwrapped — matching `mssql.ts`'s
  existing export shape; the `{ db, type }` wrapper is applied at the selection point in
  `auth.config.ts`, exactly as the comment already documents for `mssql.ts`).
  `auth.config.ts` exports `normalizeProvider` (new, mirrors the API's
  `DatabaseProviderExtensions.NormalizeProvider`) alongside the existing `database` and
  `parseEnvList`.

- [ ] **Step 1: Add `better-sqlite3`**

In `app/package.json`, add to `dependencies`: `"better-sqlite3": "^12.4.1"`, and to
`devDependencies`: `"@types/better-sqlite3": "^7.6.14"`. Run `pnpm install` from `app/`.

- [ ] **Step 2: Write the failing runtime-query test first**

`auth.config.test.ts` already has a regression-guard pattern for the adapter-shape trap
(`"is passed in the shape Better Auth expects for a pg Pool"`). Add its SQLite counterpart —
and this one runs a real query, because the proposal's Impact section is explicit that this
contract fails at query time, not compile time. Add to `app/lib/db/sqlite.test.ts` (new file):

```typescript
import { existsSync, rmSync } from "fs"
import { resolve } from "path"
import { afterEach, beforeEach, describe, expect, it } from "vitest"

const testDbPath = resolve(process.cwd(), ".sqlite-adapter-test.db")

describe("sqlite adapter", () => {
  beforeEach(() => {
    process.env.SQLITE_DATABASE_PATH = testDbPath
  })

  afterEach(() => {
    delete process.env.SQLITE_DATABASE_PATH
    for (const suffix of ["", "-wal", "-shm"]) {
      const f = testDbPath + suffix
      if (existsSync(f)) rmSync(f)
    }
  })

  it("runs a real query, not just a type-checked shape", async () => {
    // Regression guard for the same class of bug auth.config.test.ts already guards against:
    // Better Auth's `database` option is untyped, and a Pool passed where a Kysely instance is
    // expected (or vice versa) compiles fine and fails on the first query with
    // "db.selectFrom is not a function". Importing after setting the env var, not before, so
    // the lazy dialect factory opens the test file rather than the real default.
    const { db } = await import("./sqlite")
    const result = await db
      .selectFrom(db.dynamic.ref("sqlite_master").as("m") as never)
      .select((eb) => eb.lit(1).as("x"))
      .executeTakeFirst()
    expect(result).toEqual({ x: 1 })
  })
})
```

- [ ] **Step 3: Run it and confirm it fails**

Run: `pnpm --filter app test sqlite.test.ts` (or `cd app && pnpm test sqlite.test.ts`)
Expected: FAIL — `./sqlite` does not exist yet.

- [ ] **Step 4: Write `app/lib/db/sqlite.ts`**

```typescript
import { config } from 'dotenv'
import { resolve } from 'path'

// Load the shared repo-root setting before anything else — the one file both the API and
// Better Auth read to agree on which database they use.
config({ path: resolve(process.cwd(), '..', '.env') })

import { existsSync, mkdirSync } from 'fs'
import { dirname, resolve as resolvePath } from 'path'
import BetterSqlite3 from 'better-sqlite3'
import { Kysely, SqliteDialect } from 'kysely'

import { DB as Database } from "@/types"

// Matches the API's default (<repo root>/data/forgekit.db). SQLITE_DATABASE_PATH is a separate
// setting from Database__Provider on purpose — this answers "where", that one answers "which".
const sqlitePath = process.env.SQLITE_DATABASE_PATH
  ?? resolvePath(process.cwd(), '..', 'data', 'forgekit.db')

// Lazy, matching postgres.ts's Pool and mssql.ts's tarn pool: the file is not opened until a
// query actually runs. This module is imported unconditionally alongside the other two
// adapters (see auth.config.ts), so eager construction here would create/open the SQLite file
// even when a different provider is selected.
const dialect = new SqliteDialect({
    database: async () => {
        const dir = dirname(sqlitePath)
        if (!existsSync(dir)) mkdirSync(dir, { recursive: true })
        const database = new BetterSqlite3(sqlitePath)
        // WAL lets one writer proceed concurrent with readers instead of locking the whole
        // file; busy_timeout makes a writer wait for a released lock instead of failing
        // immediately with SQLITE_BUSY — both matter because the API (EF Core) opens its own,
        // independent connection to this same file.
        database.pragma('journal_mode = WAL')
        database.pragma('busy_timeout = 5000')
        return database
    },
})

export const db = new Kysely<Database>({
    dialect,
})
```

- [ ] **Step 5: Run the test again and confirm it passes**

Run: `pnpm --filter app test sqlite.test.ts` (or `cd app && pnpm test sqlite.test.ts`)
Expected: PASS.

- [ ] **Step 6: Write the failing selection-logic test**

Add to `app/lib/auth.config.test.ts`, in the existing `describe("database option", ...)` block:

```typescript
describe("normalizeProvider", () => {
    it("defaults an unset value to sqlite", async () => {
        const { normalizeProvider } = await import("./auth.config")
        expect(normalizeProvider(undefined)).toBe("sqlite")
    })

    it("is case-insensitive and accepts the API's aliases", async () => {
        const { normalizeProvider } = await import("./auth.config")
        expect(normalizeProvider("SQLite")).toBe("sqlite")
        expect(normalizeProvider("Postgres")).toBe("postgres")
        expect(normalizeProvider("PostgreSQL")).toBe("postgres")
        expect(normalizeProvider("npgsql")).toBe("postgres")
        expect(normalizeProvider("SqlServer")).toBe("sqlserver")
        expect(normalizeProvider("mssql")).toBe("sqlserver")
    })

    it("rejects an unsupported value", async () => {
        const { normalizeProvider } = await import("./auth.config")
        expect(() => normalizeProvider("Oracle")).toThrow("Unsupported database provider")
    })
})

it("selects the sqlite adapter by default", async () => {
    delete process.env.Database__Provider
    vi.resetModules()
    const { database } = await import("./auth.config")
    expect(database).toHaveProperty("type", "sqlite")
})
```

(This file already imports `describe`/`expect`/`it` from `vitest`; add `vi` to that import for
the last test's `vi.resetModules()`, needed because `auth.config.ts`'s provider selection runs
once at module load.)

- [ ] **Step 7: Run and confirm the new tests fail**

Run: `cd app && pnpm test auth.config.test.ts`
Expected: FAIL — `normalizeProvider` does not exist yet, and `database` is still always the
Postgres pool.

- [ ] **Step 8: Wire the selection into `auth.config.ts`**

Replace:
```typescript
import { db as postgresDb } from "@/lib/db/postgres"
```
with:
```typescript
import { db as mssqlDb } from "@/lib/db/mssql"
import { db as postgresDb } from "@/lib/db/postgres"
import { db as sqliteDb } from "@/lib/db/sqlite"
```
Replace the `database` constant and its preceding comment block:
```typescript
export const database = postgresDb
```
with:
```typescript
/**
 * Normalises `Database__Provider` the same way the API's own
 * `DatabaseProviderExtensions.NormalizeProvider` does, so a value that resolves on one side
 * resolves identically on the other. Defaults to `"sqlite"`, matching the API's
 * `DefaultProvider`.
 */
export function normalizeProvider(
  value: string | undefined,
): "sqlite" | "postgres" | "sqlserver" {
  const normalized = (value ?? "").trim().toLowerCase()
  switch (normalized) {
    case "":
    case "sqlite":
      return "sqlite"
    case "postgres":
    case "postgresql":
    case "npgsql":
      return "postgres"
    case "sqlserver":
    case "sql-server":
    case "mssql":
      return "sqlserver"
    default:
      throw new Error(
        `Unsupported database provider '${value}'. Supported providers: sqlite, postgres, sqlserver.`,
      )
  }
}

const selectedProvider = normalizeProvider(process.env.Database__Provider)

export const database =
  selectedProvider === "postgres"
    ? postgresDb
    : selectedProvider === "sqlserver"
      ? { db: mssqlDb, type: "mssql" as const }
      : { db: sqliteDb, type: "sqlite" as const }
```

- [ ] **Step 9: Run the tests again and confirm they pass**

Run: `cd app && pnpm test auth.config.test.ts`
Expected: PASS, including the pre-existing Postgres-shape test (it still holds when
`Database__Provider` is unset or `postgres` — confirm by also running the full suite next).

- [ ] **Step 10: Run the full frontend suite, typecheck, and lint**

Run: `cd app && pnpm test && pnpm check && pnpm lint`
Expected: all pass. `pnpm check` in particular catches a `DB` schema mismatch between
`sqlite.ts` and the existing `@/types` Kysely codegen output (they should be identical — all
three adapters target the same Better Auth schema).

- [ ] **Step 11: Commit**

```bash
git add app/package.json app/lib/db/sqlite.ts app/lib/db/sqlite.test.ts \
        app/lib/auth.config.ts app/lib/auth.config.test.ts
git commit -m "feat(app): add a SQLite adapter and select it by default

app/lib/db/sqlite.ts mirrors mssql.ts's wrapper shape and lazy-connection
pattern (better-sqlite3, opened only on first query). auth.config.ts now
picks sqlite/postgres/mssql from Database__Provider the same way the API
normalises Database:Provider, defaulting to sqlite.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
```

## Task 5: WAL + busy timeout for concurrent local writes

Delivers OpenSpec task **5.1**. Covers spec scenario *Concurrent local writes do not fail under the default provider*. The Node side (`better-sqlite3`'s `.pragma()` calls) is already done in Task 4, Step 4 — this task adds the matching EF Core side and proves the pair works together.

**Files:**
- Modify: `api/Anvil/Extensions/DatabaseProviderExtensions.cs`
- Create: `api/ForgeKit.Api.Tests/Extensions/SqlitePragmaInterceptorTests.cs`

**Interfaces:**
- Consumes: nothing new.
- Produces: `SqlitePragmaInterceptor` (internal to `DatabaseProviderExtensions.cs`), registered
  only when the resolved provider is `"Sqlite"`.

- [ ] **Step 1: Write the failing EF Core-side test**

Create `api/ForgeKit.Api.Tests/Extensions/SqlitePragmaInterceptorTests.cs`:

```csharp
using Anvil.Data;
using Anvil.Extensions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ForgeKit.Api.Tests.Extensions;

public sealed class SqlitePragmaInterceptorTests
{
    [Fact]
    public async Task OpeningAConnection_SetsWalJournalModeAndBusyTimeout()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");
        try
        {
            var options = new DbContextOptionsBuilder<PlatformDbContext>()
                .UseSqlite($"Data Source={dbPath}")
                .Options;

            // PlatformDbContext is abstract; a minimal concrete context here is enough to open
            // a real connection through the same options-building path production code uses —
            // constructing it directly, not through AddConfiguredDbContext, since only the
            // interceptor registration is under test.
            await using var connection = new SqliteConnection($"Data Source={dbPath}");
            DatabaseProviderExtensions.ApplySqlitePragmas(connection);

            await using var journalCmd = connection.CreateCommand();
            journalCmd.CommandText = "PRAGMA journal_mode;";
            var journalMode = (string)(await journalCmd.ExecuteScalarAsync())!;
            journalMode.ShouldBe("wal", StringCompareShould.IgnoreCase);

            await using var timeoutCmd = connection.CreateCommand();
            timeoutCmd.CommandText = "PRAGMA busy_timeout;";
            var busyTimeout = Convert.ToInt32(await timeoutCmd.ExecuteScalarAsync());
            busyTimeout.ShouldBeGreaterThan(0);
        }
        finally
        {
            foreach (var suffix in new[] { "", "-wal", "-shm" })
            {
                var f = dbPath + suffix;
                if (File.Exists(f)) File.Delete(f);
            }
        }
    }
}
```

- [ ] **Step 2: Run it and confirm it fails**

Run: `dotnet test api/ForgeKit.Api.Tests --filter SqlitePragmaInterceptorTests`
Expected: FAIL — `DatabaseProviderExtensions.ApplySqlitePragmas` does not exist yet.

- [ ] **Step 3: Add the pragma application and register it for the SQLite provider only**

In `api/Anvil/Extensions/DatabaseProviderExtensions.cs`, add an `internal static` helper and wire
it into `ConfigureProvider`'s `Sqlite` case:

```csharp
case "Sqlite":
    options.UseSqlite(settings.ConnectionString);
    options.AddInterceptors(new SqlitePragmaInterceptor());
    break;
```

Add the helper and interceptor near the bottom of the file, after `NormalizeProvider`:

```csharp
// WAL lets one writer proceed concurrent with readers instead of locking the whole database
// file for the duration of a write; a non-zero busy_timeout makes a writer wait for a released
// lock instead of failing immediately with SQLITE_BUSY. Both matter because the frontend
// (better-sqlite3, app/lib/db/sqlite.ts) opens its own, independent connection to this same
// file — see openspec/specs/database-provider/spec.md, "Concurrent local writes do not fail".
internal static void ApplySqlitePragmas(System.Data.Common.DbConnection connection)
{
    using var command = connection.CreateCommand();
    command.CommandText = "PRAGMA journal_mode=WAL; PRAGMA busy_timeout=5000;";
    command.ExecuteNonQuery();
}

file sealed class SqlitePragmaInterceptor : Microsoft.EntityFrameworkCore.Diagnostics.DbConnectionInterceptor
{
    public override void ConnectionOpened(
        System.Data.Common.DbConnection connection,
        Microsoft.EntityFrameworkCore.Diagnostics.ConnectionEndEventData eventData)
        => ApplySqlitePragmas(connection);

    public override async Task ConnectionOpenedAsync(
        System.Data.Common.DbConnection connection,
        Microsoft.EntityFrameworkCore.Diagnostics.ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA journal_mode=WAL; PRAGMA busy_timeout=5000;";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
```

(`ApplySqlitePragmas` is `internal`, not `private`, specifically so the test in Step 1 can call
it directly against a plain `SqliteConnection` without needing a full DbContext.)

- [ ] **Step 4: Run the test again and confirm it passes**

Run: `dotnet test api/ForgeKit.Api.Tests --filter SqlitePragmaInterceptorTests`
Expected: PASS.

- [ ] **Step 5: Prove the cross-runtime claim — a real concurrent-write smoke test**

Not a committed automated test (a two-process, two-runtime race is too heavy and potentially
flaky for the regular suite) — run once, in this session, and record the result rather than
assume the two pragma sites add up to the guarantee the spec describes:

```bash
rm -f /tmp/wal-check.db*
node -e '
const Database = require("./app/node_modules/better-sqlite3");
const db = new Database("/tmp/wal-check.db");
db.pragma("journal_mode = WAL");
db.pragma("busy_timeout = 5000");
db.exec("CREATE TABLE IF NOT EXISTS t (id INTEGER PRIMARY KEY, v TEXT)");
for (let i = 0; i < 200; i++) db.prepare("INSERT INTO t (v) VALUES (?)").run("node-" + i);
console.log("node: 200 writes done");
' &
NODE_PID=$!
cat > /tmp/WalCheck.csx <<'CS'
using Microsoft.Data.Sqlite;
using var connection = new SqliteConnection("Data Source=/tmp/wal-check.db");
connection.Open();
using (var pragma = connection.CreateCommand()) { pragma.CommandText = "PRAGMA journal_mode=WAL; PRAGMA busy_timeout=5000;"; pragma.ExecuteNonQuery(); }
using (var create = connection.CreateCommand()) { create.CommandText = "CREATE TABLE IF NOT EXISTS t (id INTEGER PRIMARY KEY, v TEXT)"; create.ExecuteNonQuery(); }
for (var i = 0; i < 200; i++)
{
    using var insert = connection.CreateCommand();
    insert.CommandText = "INSERT INTO t (v) VALUES ($v)";
    insert.Parameters.AddWithValue("$v", $"dotnet-{i}");
    insert.ExecuteNonQuery();
}
Console.WriteLine("dotnet: 200 writes done");
CS
dotnet script /tmp/WalCheck.csx 2>&1 || dotnet-script /tmp/WalCheck.csx 2>&1
wait $NODE_PID
sqlite3 /tmp/wal-check.db "select count(*) from t;"
```
Expected: no `SQLITE_BUSY` / "database is locked" error from either side, and the final count is
400 (200 + 200 — nothing lost). If `dotnet script`/`dotnet-script` is not installed, substitute
a throwaway xUnit test with the same loop instead of installing a new global tool — the point is
running both runtimes against the same file concurrently at least once, not the exact vehicle.

- [ ] **Step 6: Run the full backend and frontend suites**

Run: `dotnet test` and `cd app && pnpm test`
Expected: both pass.

- [ ] **Step 7: Commit**

```bash
git add api/Anvil/Extensions/DatabaseProviderExtensions.cs \
        api/ForgeKit.Api.Tests/Extensions/SqlitePragmaInterceptorTests.cs
git commit -m "feat(api): set WAL journal mode and a busy timeout on SQLite connections

Matches the pragma pair already set on the frontend's better-sqlite3
connection (app/lib/db/sqlite.ts) so the API's independent EF Core
connection to the same file does not lose or block on a concurrent write.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
```

## Task 6: Documentation

Delivers OpenSpec task **6.1**. No code interfaces — prose only, so no test-first cycle; each edit is verified by grep for the phrase it retires.

**Files:**
- Modify: `README.md`, `docs/FORKING_GUIDE.md`, `docs/LOCAL_DEVELOPMENT.md`

- [ ] **Step 1: `README.md`**

Replace:
```
- **PostgreSQL** for the frontend's Better Auth database

The API uses SQLite by default and does not require an external database. PostgreSQL and SQL Server are optional API provider choices. The frontend currently uses Better Auth's PostgreSQL adapter and therefore needs PostgreSQL for authentication flows.
```
with:
```
Neither the API nor the frontend requires an external database to start — both default to one
shared SQLite file at `<repo root>/data/forgekit.db`. PostgreSQL and SQL Server are opt-in for
both sides together, switched with a single setting.

The API uses SQLite by default. The frontend's Better Auth instance uses the same provider —
never a different one — selected by `Database__Provider` in a repository-root `.env` (copy
`.env.example`). Editing only `app/.env.local` or only `api/**/appsettings*.json` no longer has
any effect on which provider either side uses.
```

- [ ] **Step 2: `docs/FORKING_GUIDE.md`**

Replace:
```
- `DATABASE_URL`: required by the frontend Better Auth PostgreSQL adapter
- `Database:Provider`: API provider, defaults to `Sqlite`
```
with:
```
- `Database__Provider` (repository-root `.env`, copy from `.env.example`): the one setting that
  selects the provider for both the API and Better Auth — `Sqlite` (default), `Postgres`, or
  `SqlServer`
- `DATABASE_URL`: only read when `Database__Provider` is `Postgres`
```

Replace:
```
The frontend currently uses Better Auth with PostgreSQL, so auth flows need PostgreSQL even if the API stays on SQLite.
```
with:
```
The frontend and the API always use the same provider — `Database__Provider` in the
repository-root `.env` selects it for both. There is no configuration that leaves one side on
SQLite while the other needs PostgreSQL.
```

- [ ] **Step 3: `docs/LOCAL_DEVELOPMENT.md`**

Replace:
```
ForgeKit is designed to start with minimal infrastructure:

- API: SQLite by default
- App: PostgreSQL for Better Auth

## Start PostgreSQL

The compose file starts a local PostgreSQL instance for the frontend auth database.

```bash
docker compose up -d postgres
docker compose ps
```

The starter uses local-only trust authentication so no shared password is committed. The matching frontend connection string is:

```bash
DATABASE_URL=postgresql://localhost:5432/forgekit
```
```
with:
```
ForgeKit is designed to start with minimal infrastructure: both the API and the frontend
default to one shared SQLite file at `<repo root>/data/forgekit.db` — no service to start.

## Configure The Shared Provider Setting (optional)

```bash
cp .env.example .env
```

The default, `Database__Provider=Sqlite`, needs no further setup — skip to "Configure The App".
To use PostgreSQL or SQL Server instead, set `Database__Provider` here (`Postgres` or
`SqlServer`) — this one file is read by both the API and the frontend, so there is nothing else
to keep in sync.

## Start PostgreSQL (only if `Database__Provider=Postgres`)

The compose file starts a local PostgreSQL instance shared by both sides.

```bash
docker compose up -d postgres
docker compose ps
```

The starter uses local-only trust authentication so no shared password is committed. The
matching frontend connection string, read only when `Database__Provider=Postgres`:

```bash
DATABASE_URL=postgresql://localhost:5432/forgekit
```
```

Replace:
```
The API uses SQLite by default and does not need PostgreSQL.
```
with:
```
The API reads the same `Database__Provider` from the repository-root `.env` as the frontend —
nothing further to configure for the SQLite default.
```

- [ ] **Step 4: Verify the old framing is gone**

Run:
```bash
grep -rn "needs PostgreSQL\|need PostgreSQL even if\|App: PostgreSQL for Better Auth" \
  README.md docs/FORKING_GUIDE.md docs/LOCAL_DEVELOPMENT.md
```
Expected: no output.

- [ ] **Step 5: Commit**

```bash
git add README.md docs/FORKING_GUIDE.md docs/LOCAL_DEVELOPMENT.md
git commit -m "docs: describe SQLite as the true zero-service default for both sides

Retires the previous framing (frontend needs PostgreSQL even when the
API stays on SQLite) and documents the repo-root .env / Database__Provider
as the one switch that moves both sides together.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
```

## Task 7: End-to-end verification through the template

Delivers OpenSpec task **7.1**. Covers spec scenario *The API and Better Auth share one SQLite database* end to end — demonstrated against a real, freshly generated product, not the source repo (which already has stray state from Tasks 1–6's manual runs).

**Files:** none — this task only runs and observes.

- [ ] **Step 1: Reinstall the template and generate a fresh product**

```bash
dotnet new uninstall /Users/zuexx/Documents/labs/forgekit-family/forgekit 2>/dev/null
dotnet new install /Users/zuexx/Documents/labs/forgekit-family/forgekit --force
rm -rf /tmp/forgekit-e2e-check
dotnet new forgekit -n Acme.Portal --slug acmeportal -o /tmp/forgekit-e2e-check
```
Expected: generation succeeds, `--force` picked up every change from Tasks 1–6.

- [ ] **Step 2: Build and migrate the generated API**

```bash
cd /tmp/forgekit-e2e-check/api
dotnet restore && dotnet build
dotnet ef database update --project Acme.Portal.Api.Migrations.Sqlite \
  --startup-project Acme.Portal.Api.Migrations.Sqlite --context AppDbContext
dotnet ef database update --project Acme.Portal.Api.Migrations.Sqlite \
  --startup-project Acme.Portal.Api.Migrations.Sqlite --context BetterAuthDbContext
dotnet test
```
Expected: 0 build errors; both migration commands succeed; full test suite passes — record the
count in the final report.

- [ ] **Step 3: Set up and build the generated frontend**

```bash
cd /tmp/forgekit-e2e-check/app
cp .env.local.example .env.local
sed -i '' "s/^BETTER_AUTH_SECRET=.*/BETTER_AUTH_SECRET=$(openssl rand -base64 32)/" .env.local
cd /tmp/forgekit-e2e-check
cp .env.example .env   # Database__Provider=Sqlite — the default, left as-is
cd app && pnpm install && pnpm check && pnpm lint && pnpm build
```
Expected: 0 errors.

- [ ] **Step 4: Start both processes**

```bash
(cd /tmp/forgekit-e2e-check/api/Acme.Portal.Api && dotnet run --no-launch-profile >/tmp/e2e-api.log 2>&1 &)
sleep 8
(cd /tmp/forgekit-e2e-check/app && pnpm dev >/tmp/e2e-app.log 2>&1 &)
sleep 8
```
Expected: `/tmp/e2e-api.log` shows `Database provider: Sqlite` (the log line added in Task 3);
`/tmp/e2e-app.log` shows the dev server listening.

- [ ] **Step 5: Register through Better Auth and read the row back from the shared file**

```bash
curl -s -X POST http://localhost:3000/api/auth/sign-up/email \
  -H 'Content-Type: application/json' \
  -d '{"email":"e2e-check@example.com","password":"a-real-password-1","name":"E2E Check"}' \
  | tee /tmp/e2e-signup-response.json

sqlite3 /tmp/forgekit-e2e-check/data/forgekit.db \
  "select id, email, name from user where email = 'e2e-check@example.com';"
```
Expected: the sign-up response is a success (a session/user payload, not an error); the
`sqlite3` query returns exactly one row for `e2e-check@example.com` — proving the row Better
Auth (Node) wrote is readable from the same file the API's `BetterAuthDbContext` (the .NET
side) is configured against, without restarting either process. This is the scenario the whole
change exists to make true — observe it, do not infer it from both sides merely "being
configured for SQLite."

- [ ] **Step 6: Confirm the schema stayed separate**

```bash
sqlite3 /tmp/forgekit-e2e-check/data/forgekit.db ".tables"
```
Expected: both the `user`/`session`/`account`/`jwks`/`verification` tables (Better Auth) and the
product tables (`Workspace`, `TodoItem`, etc. — whatever `AppDbContext` defines) are present in
the one file, under distinct names — the SQLite half of "sharing an instance does not merge the
schemas."

- [ ] **Step 7: Clean up**

```bash
pkill -f "Acme.Portal.Api" 2>/dev/null || true
pkill -f "next dev" 2>/dev/null || true
podman compose -f /Users/zuexx/Documents/labs/forgekit-family/forgekit/compose.yaml down 2>&1 || true
rm -rf /tmp/forgekit-e2e-check /tmp/e2e-*.log /tmp/e2e-signup-response.json /tmp/wal-check.db* \
  /tmp/no-env-run.log /tmp/provider-check*.log /tmp/WalCheck.csx
git -C /Users/zuexx/Documents/labs/forgekit-family/forgekit status --porcelain  # source repo unaffected
```
Expected: the source repo's `git status` shows only what Tasks 1–6 committed — nothing left
over from generating and running the throwaway product.

- [ ] **Step 8: Record the result — this is the report, not a commit**

No files change in this task. Note in the session's report to the user: the generated-product
build/migrate/test result and count from Step 2, and that Steps 5–6 were observed (row written
by Node, read by direct query against the same file the API is configured for; both schemas
present and distinct) rather than assumed.

## Self-Review

**Spec coverage** — every scenario in `specs/database-provider/spec.md`'s delta maps to a task:

| Scenario | Task |
| --- | --- |
| Local Better Auth uses SQLite | 4 (default selection) |
| Production Better Auth uses selected provider | 1 (schema) + 4 (selection) |
| The API and Better Auth share one SQLite database | 2 (file location) + 4 (adapter) + 7 (proven live) |
| One setting selects the provider for both sides | 3 (root `.env`) + 4 (frontend reads it) |
| Concurrent local writes do not fail under the default provider | 5 |
| Sharing an instance does not merge the schemas | 1 (schema) + 7 (proven live, Step 6) |

**Placeholder scan** — no `TBD`/`TODO`; every code step carries real, complete file content;
Task 5's cross-runtime smoke test names a concrete fallback if `dotnet-script` is unavailable
rather than leaving it unresolved.

**Type/name consistency** — `Database__Provider` is the literal key used identically in
`.env.example`, `Program.cs`'s `DotNetEnv` read (via ASP.NET Core's double-underscore binding to
`Database:Provider`), and `auth.config.ts`'s `process.env.Database__Provider`. `normalizeProvider`
(TS) and `NormalizeProvider`/`GetDatabaseProviderSettings` (C#, unchanged) accept the same alias
set (`sqlite`, `postgres`/`postgresql`/`npgsql`, `sqlserver`/`sql-server`/`mssql`) —
cross-checked against `DatabaseProviderExtensionsTests.cs`'s existing `[Theory]` data in Task 1's
grounding, not just written from memory. `ApplySqlitePragmas` is used identically by the
production interceptor and its own test (Task 5).

**Execution mode** — Tasks are sequential and each depends on state the previous one left
(schema before file-move is independent, but 3 before 4 is not — the frontend adapter reads the
setting Task 3 introduces). `superpowers:subagent-driven-development`'s decision tree routes
this tightly-coupled sequence to inline execution, not parallel subagents. Request review with
`superpowers:requesting-code-review` after Task 7, before ticking any OpenSpec task.

**A risk this plan does not fully close, named rather than hidden:** Task 1 Step 8's SQL Server
live verification is explicitly best-effort — if the container does not come up cleanly, the
schema-separation guarantee for SQL Server rests on the model-level test and the generated
migration's content, not a live `CREATE TABLE ... IN SCHEMA auth` having actually run. Surface
this plainly in the final report if it happens; do not report SQL Server as "verified" without
qualification in that case.

# Local Development

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

## Configure The App

```bash
cd app
cp .env.local.example .env.local
```

Set `BETTER_AUTH_SECRET` in `.env.local`:

```bash
openssl rand -base64 32
```

Then run:

```bash
pnpm install
pnpm dev
```

## Configure The API

The API reads the same `Database__Provider` from the repository-root `.env` as the frontend —
nothing further to configure for the SQLite default.

```bash
cd api
dotnet restore
dotnet tool restore
dotnet ef database update --project ForgeKit.Api.Migrations.Sqlite --startup-project ForgeKit.Api.Migrations.Sqlite --context AppDbContext
dotnet ef database update --project ForgeKit.Api.Migrations.Sqlite --startup-project ForgeKit.Api.Migrations.Sqlite --context BetterAuthDbContext
dotnet run --project ForgeKit.Api
```

To use PostgreSQL or SQL Server, set `Database__Provider` in the repository-root `.env` (not
`appsettings.Local.json` — that changes only the API) and run migrations from the matching
provider project.

## Stop Local Services

```bash
docker compose down
```

To delete the local PostgreSQL data volume:

```bash
docker compose down --volumes
```

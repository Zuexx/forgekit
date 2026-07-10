# Local Development

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

The API uses SQLite by default and does not need PostgreSQL.

```bash
cd api
dotnet restore
dotnet tool restore
dotnet ef database update --project ForgeKit.Api.Migrations.Sqlite --startup-project ForgeKit.Api.Migrations.Sqlite --context AppDbContext
dotnet ef database update --project ForgeKit.Api.Migrations.Sqlite --startup-project ForgeKit.Api.Migrations.Sqlite --context BetterAuthDbContext
dotnet run --project ForgeKit.Api
```

To use PostgreSQL or SQL Server for the API, copy `api/ForgeKit.Api/appsettings.Local.json.example` to `appsettings.Local.json`, set `Database:Provider`, and run migrations from the matching provider project.

## Stop Local Services

```bash
docker compose down
```

To delete the local PostgreSQL data volume:

```bash
docker compose down --volumes
```

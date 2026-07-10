# Change: Add Configurable Database Provider

## Why
The starter kit currently assumes PostgreSQL for runtime persistence. That makes first-run setup and migration checks depend on an external database, which is friction for forks and local evaluation. The default development path should run without external infrastructure while still allowing production projects to choose PostgreSQL or SQL Server.

## What Changes
- Add a configurable database provider setting with `Sqlite` as the default.
- Support `Postgres` and `SqlServer` as explicit provider options.
- Apply the same provider selection to both `AppDbContext` and `BetterAuthDbContext` so Better Auth has durable local storage.
- Use SQLite for local development and CI smoke paths, not EF Core InMemory.
- Keep EF Core InMemory only for focused tests where relational behavior is not required.
- Split provider-specific EF migrations so SQLite, PostgreSQL, and SQL Server schemas can evolve safely.
- Update configuration and onboarding docs for provider selection and connection strings.

## Impact
- Affected specs: `database-provider`, `data-model`, `foundations`
- Affected code: `Api.csproj`, `Program.cs`, DbContext registration, appsettings, migrations, docs, tests
- Database: default local database becomes SQLite file-based storage; PostgreSQL remains supported by configuration
- Breaking changes: local default connection settings change from PostgreSQL to SQLite

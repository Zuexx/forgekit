# Change: Add Provider-Specific Migration Projects

## Why
ForgeKit configures SQLite, PostgreSQL, and SQL Server at runtime, but only ships SQLite migrations. The current documentation asks forks to replace the SQLite-only design-time factory, which makes provider support incomplete and risks mixing migrations for different SQL dialects in one assembly.

## What Changes
- Add dedicated EF Core migration projects for SQLite, PostgreSQL, and SQL Server.
- Keep `AppDbContext` and `BetterAuthDbContext` migrations isolated by provider.
- Move the existing SQLite migrations into the SQLite migration project without changing their migration identifiers.
- Generate initial PostgreSQL and SQL Server migrations without requiring live database servers.
- Add provider-aware design-time factories and explicit migration commands.
- Verify every provider exposes exactly one migration chain per DbContext.

## Impact
- Affected specs: `database-provider`
- Affected code: `api/ForgeKit.sln`, EF Core migrations, design-time factories, database provider configuration, tests, CI, and database documentation
- Database: existing SQLite migration identifiers remain stable; PostgreSQL and SQL Server receive independent initial migration histories
- Breaking changes: migration CLI commands must specify the provider migration project

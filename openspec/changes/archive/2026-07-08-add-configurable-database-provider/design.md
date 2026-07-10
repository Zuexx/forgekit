## Context
The project is intended to be forked as a starter kit. Requiring PostgreSQL before the first successful run makes migrations, Better Auth local sessions, and smoke checks brittle. EF Core InMemory is not a suitable runtime default because it does not persist Better Auth data and does not behave like a relational database.

## Goals
- Make a fresh fork runnable with no external database service.
- Preserve PostgreSQL and SQL Server as supported production-ready options.
- Use one configuration model for application and Better Auth DbContexts.
- Keep provider-specific migrations isolated.
- Make CI and local smoke checks deterministic with SQLite.

## Non-Goals
- Remove PostgreSQL support.
- Use EF Core InMemory as a runtime provider.
- Automatically migrate production databases at startup.
- Add cross-provider abstractions beyond DbContext registration and migrations.

## Decisions
- Default provider: `Sqlite`.
- Supported providers: `Sqlite`, `Postgres`, `SqlServer`.
- Configuration shape:
  ```json
  {
    "Database": {
      "Provider": "Sqlite"
    },
    "ConnectionStrings": {
      "Sqlite": "Data Source=./data/scaffold.db",
      "Postgres": "Host=localhost;Database=scaffold_db",
      "SqlServer": "Server=localhost;Database=scaffold_db;Trusted_Connection=True;TrustServerCertificate=True"
    }
  }
  ```
- `AppDbContext` and `BetterAuthDbContext` must use the same provider by default.
- The starter kit ships compiled SQLite migrations for the default local path.
- PostgreSQL and SQL Server are supported providers, but their migrations are generated on demand per fork. EF Core does not isolate multiple migration sets for one DbContext by folder alone inside a single assembly.
- Tests should prefer SQLite in-memory connections for relational behavior and EF Core InMemory only for narrow unit tests.

## Risks / Trade-offs
- Maintaining multiple provider migrations adds overhead.
- SQLite differs from PostgreSQL and SQL Server in type system and concurrency behavior.
- Forks that switch providers must generate provider-specific migrations before deploying.

## Migration Plan
1. Add provider packages and provider-selection registration.
2. Change default config to SQLite file storage.
3. Generate SQLite migrations for the default starter path.
4. Update tests and smoke checks to use SQLite for relational validation.
5. Update docs and starter onboarding.
6. Validate local SQLite migration/update without external services.

## Resolved Questions
- SQL Server ships as provider registration plus docs first; migrations are generated when a fork selects SQL Server.
- Local SQLite database files live under `api/Api/data/`.

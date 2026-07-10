## 1. Provider Configuration
- [x] 1.1 Add `Database:Provider` setting with `Sqlite` default
- [x] 1.2 Add `ConnectionStrings:Sqlite`, `ConnectionStrings:Postgres`, and `ConnectionStrings:SqlServer`
- [x] 1.3 Ensure local database files are gitignored
- [x] 1.4 Remove real or environment-specific connection strings from committed examples

## 2. EF Core Provider Support
- [x] 2.1 Add `Microsoft.EntityFrameworkCore.Sqlite`
- [x] 2.2 Add `Microsoft.EntityFrameworkCore.SqlServer`
- [x] 2.3 Keep `Npgsql.EntityFrameworkCore.PostgreSQL`
- [x] 2.4 Implement provider selection for `AppDbContext`
- [x] 2.5 Implement provider selection for `BetterAuthDbContext`
- [x] 2.6 Fail fast with a clear error for unknown providers or missing connection strings

## 3. Migrations
- [x] 3.1 Create SQLite migrations for `AppDbContext`
- [x] 3.2 Create SQLite migrations for `BetterAuthDbContext`
- [x] 3.3 Document PostgreSQL migration generation for forks that switch providers
- [x] 3.4 Document SQL Server deferred migration generation
- [x] 3.5 Verify `dotnet ef database update` succeeds for SQLite without external DB services

## 4. Tests And Smoke Checks
- [x] 4.1 Update integration tests that need relational behavior to use SQLite
- [x] 4.2 Keep EF Core InMemory only for focused unit tests
- [x] 4.3 Add provider selection tests for SQLite, PostgreSQL, SQL Server, and invalid provider
- [x] 4.4 Add a starter-kit smoke command that validates SQLite setup

## 5. Documentation
- [x] 5.1 Update README / starter onboarding for SQLite default
- [x] 5.2 Update configuration guide with provider examples
- [x] 5.3 Document Better Auth local persistence expectations
- [x] 5.4 Document production switch to PostgreSQL or SQL Server
- [x] 5.5 Document migration commands per provider

## 6. Validation
- [x] 6.1 Run `openspec validate add-configurable-database-provider --strict --no-interactive`
- [x] 6.2 Run `dotnet build --no-restore --no-incremental`
- [x] 6.3 Run `dotnet test --no-restore`
- [x] 6.4 Run SQLite migration/update smoke test

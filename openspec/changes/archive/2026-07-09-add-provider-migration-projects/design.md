## Context
EF Core discovers migrations by DbContext and migrations assembly. Directory names and namespaces do not isolate multiple provider migration chains inside one assembly. ForgeKit currently stores SQLite migrations in `ForgeKit.Api` and has SQLite-only design-time factories, so PostgreSQL and SQL Server cannot safely generate or apply independent migrations.

## Goals / Non-Goals
- Goals: ship valid migration chains for all supported providers, preserve SQLite migration identifiers, keep provider DDL isolated, and provide deterministic CLI and CI checks.
- Non-Goals: automatically migrate databases at API startup, require database containers in the default CI path, or abstract provider-specific SQL features.

## Decisions
- Create `ForgeKit.Api.Migrations.Sqlite`, `ForgeKit.Api.Migrations.Postgres`, and `ForgeKit.Api.Migrations.SqlServer`.
- Each migration project references `ForgeKit.Api`, contains design-time factories for both DbContexts, and configures its own assembly with `MigrationsAssembly`.
- Migration projects are CLI/deployment artifacts. The API does not reference them and does not invoke `Database.Migrate()` at startup, avoiding circular project references.
- Move the current SQLite migration source files to the SQLite project and retain their migration IDs.
- Generate initial PostgreSQL and SQL Server migrations from the same EF models. Generation does not require a reachable server.
- Commands use the same migration project for both `--project` and `--startup-project`; provider selection is therefore explicit and EF can discover the project's design-time factories.
- CI lists migrations with `--no-connect` for both DbContexts in every provider project.

## Risks / Trade-offs
- Six migration chains must be maintained instead of two. CI migration discovery checks catch accidental omissions or cross-provider discovery.
- Model changes require generating migrations in all three projects. Documentation and review checklists make this explicit.
- Provider-specific model differences may emerge. They remain local to the corresponding migration project rather than leaking into shared migration history.

## Migration Plan
1. Create the three migration projects and add them to the solution.
2. Move SQLite migrations while preserving identifiers and snapshots.
3. Remove SQLite-only factories from the API project.
4. Generate PostgreSQL and SQL Server initial migrations.
5. Update tests, CI, and documentation to use explicit provider projects.
6. Validate builds, tests, migration discovery, and a fresh SQLite database update.

Rollback consists of restoring the SQLite migrations and design-time factories to `ForgeKit.Api`; no existing database migration identifiers need to change.

## Open Questions
- None.

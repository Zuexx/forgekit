# Samples

ForgeKit keeps a small reference domain so forks can see the project conventions working end to end. These samples are intentionally generic and should not be treated as product business logic.

## What Is Sample Code

### Endpoint And CQRS Sample

Files:

- `api/ForgeKit.Api/Modules/SampleResourceModule.cs`
- `api/ForgeKit.Api/Samples/`
- `api/ForgeKit.Api.Tests/Modules/SampleResourceModuleTests.cs`
- `api/ForgeKit.Api.Tests/Samples/`

Purpose:

- Minimal API module registration
- `ISampleModule` production filtering
- MediatR command/query handlers
- FluentValidation
- Result-to-HTTP mapping
- API integration tests

Production behavior:

- Modules implementing `ISampleModule` are registered for services but are not mapped to endpoints when `IHostEnvironment.IsProduction()` is true.
- `SampleResourceModule` exposes `/v1/resources` only outside production.

### Todo Reference Domain

Files:

- `api/ForgeKit.Api/Entities/Todos/`
- `api/ForgeKit.Api/Services/Todos/TodoService.cs`
- `api/ForgeKit.Api.Tests/Services/TodoServiceSoftDeleteTests.cs`
- related EF Core migrations in every provider migration project

Purpose:

- EF Core entity mapping
- relationships to Workspace, Member, Category, and audit base entities
- Unit of Work usage
- audit context propagation
- soft delete and restore conventions
- status-history modeling

Production behavior:

- Todo entities are part of `AppDbContext` and provider migrations.
- Unlike `ISampleModule` endpoints, Todo tables are included in the database schema until the fork removes them and adds replacement migrations.

## What To Keep In A Fork

Keep the samples when the fork is still being used as a platform base or training reference. They make conventions testable and give future contributors working examples.

Remove or replace the samples when the fork becomes a product and has its first real feature using the same patterns.

## Removing Samples From A Product Fork

Recommended order:

1. Add a real feature that demonstrates module, handler, validation, Result, persistence, and tests.
2. Delete `SampleResourceModule` and `api/ForgeKit.Api/Samples/`.
3. Delete Todo entities and `TodoService` if they are not part of the product domain.
4. Remove sample tests.
5. Create provider migrations that remove sample tables from every supported migration project.
6. Update `docs/STRUCTURE.md`, `docs/api/USER_GUIDE.md`, and `openspec/specs/data-model/spec.md`.
7. Run the full quality gates.

Do not silently leave sample endpoints or sample tables in a production product without documenting why they remain.

## Adding New Samples

New samples should be:

- clearly named with `Sample` or documented as a reference domain
- domain-neutral
- covered by focused tests
- excluded from production endpoints with `ISampleModule` when they expose routes
- represented in OpenSpec if they change accepted behavior

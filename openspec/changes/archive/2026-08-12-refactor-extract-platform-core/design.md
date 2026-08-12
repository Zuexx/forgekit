# Design: Product-Neutral Platform Core

## Classification Principle

A file belongs to `Anvil` when it would be byte-identical in every product generated from the base. The test applied here is mechanical rather than aesthetic: a file is core only if its transitive `using ForgeKit.Api.*` closure contains no product type. Product types are the domain entities (`Entities/{Core,Configuration,Todos,Analytics}`), the modules that expose them, and the services that operate on them.

Classification below is derived from the actual import graph of all 64 source files, not from directory names.

## Layer A — `Anvil` (41 files, name never changes)

| Area | Files | Why core |
|---|---|---|
| `Constants/` | `AppSettingKeys.cs`, `ErrorCodes.cs` | No imports |
| `Exceptions/` | all 7 | No imports |
| `Results/` | `Result.cs`, `ResultExtensions.cs` | No imports |
| `Models/` | `AuthorizedUser.cs`, `ErrorResponse.cs`, `JwtSetupData.cs` | No imports |
| `Entities/Base/` | `BaseEntity.cs`, `IAuditableEntity.cs`, `ISoftDelete.cs` | Contracts, no imports |
| `Entities/Auth/` | `Account.cs`, `Jwk.cs`, `Session.cs`, `User.cs`, `Verification.cs` | Better Auth schema, identical per product |
| `Interfaces/` | `IAuditContext.cs`, `IJwksProvider.cs`, `IModule.cs`, `IRootModule.cs` | No imports |
| `Middlewares/` | `CorrelationIdMiddleware.cs`, `ExceptionHandlingMiddleware.cs` | Depends only on Constants/Exceptions/Models |
| `Behaviors/` | `ValidationBehavior.cs` | Depends only on Exceptions |
| `Handlers/` | `ResultCommandHandler.cs`, `ResultQueryHandler.cs` | Depend only on Results |
| `Extensions/` | `CorsExtensions.cs`, `ResultEndpointExtensions.cs`, `HttpContextAccessorExtension.cs`, `ConfigureJwtBearerOptions.cs`, `ModuleExtension.cs` | Depend only on core areas |
| `Foundations/` | `JwksProvider.cs` | Depends only on Interfaces |
| `Domain/Services/` | `SoftDeleteDomainService.cs` | Operates on `ISoftDelete`, not on concrete entities |
| `Services/` | `AuditContextService.cs` | Depends only on Interfaces |
| `Data/Auth/` | `BetterAuthDbContext.cs` | Depends only on `Entities/Auth` |
| `Modules/` | `HealthModule.cs` | Depends only on `IModule` |

## Layer B — `<Product>.Api` (17 files, renamed per product)

| Area | Files | Why product |
|---|---|---|
| Composition root | `Program.cs` | Wires the specific product |
| `Entities/Core/` | `Workspace.cs`, `Member.cs` | Product domain |
| `Entities/Configuration/` | `Category.cs`, `Label.cs`, `CategoryLabel.cs` | Product domain |
| `Entities/Todos/` | `TodoItem.cs`, `TodoStatusHistory.cs` | Sample domain |
| `Entities/Analytics/` | `WorkspaceAnalytics.cs`, `DailyActivitySnapshot.cs` | Product domain |
| `Modules/` | `SampleResourceModule.cs` | Sample endpoints |
| `Samples/` | all 4 | Sample handlers and validator |
| `Services/Todos/` | `TodoService.cs` | Sample domain service |
| `Foundations/` | `PocDataSeeder.cs` | Seeds product entities |

## Layer C — 7 files that couple the layers

Each of these must be split or generalised. C2 and C6 are settled; C7 was found while resolving C6 and is a blocker.

### C1. `Data/AppDbContext.cs` (326 lines)

Imports every product entity namespace. Splits cleanly along existing private methods:

- To core, as `abstract class PlatformDbContext : DbContext` — `ConfigureSoftDeleteFilters` (reflection over `ISoftDelete`) and `ConfigureCamelCaseNames` (naming convention). Both are already product-neutral.
- To product, as `AppDbContext : PlatformDbContext` — the 9 `DbSet<>` declarations, `ConfigureRelationships`, `ConfigureIndexes`, `ConfigureJsonColumns`.

`ConfigureJsonColumns` is mixed: the decimal-precision block is generic, the JSON column mapping is entity-specific. Proposal: move the decimal-precision loop to core, leave the rest in the product.

### C2. `Interfaces/IUnitOfWork.cs` — **DECIDED: (b)**

`AppDbContext DbContext { get; }` returns the concrete product type. This single line is what pins the Unit of Work to the product. Options considered:

| Option | Cost | Consequence |
|---|---|---|
| (a) Return base `DbContext` | Lowest | Callers lose typed `DbSet` access and must call `Set<T>()`; touches every service that uses `uow.DbContext.TodoItems` |
| **(b) `IUnitOfWork<TContext> where TContext : PlatformDbContext`** | Medium | Keeps type safety; every injection site and DI registration gains a type argument |
| (c) Keep `IUnitOfWork` in the product layer | Zero refactor | Unit of Work stops being shared — upstream improvements to it no longer propagate |

Decision: **(b)**. It preserves the typed access the current services rely on, and the noise is confined to declaration sites. (a) looks cheaper but pushes untyped `Set<T>()` calls into product code permanently. (c) surrenders exactly the kind of file this change exists to share.

### C3. `Data/UnitOfWork.cs`

Follows C2 mechanically. No independent decision.

### C4. `Extensions/ServiceExtension.cs`

Currently registers `TodoService` (product) alongside `IAuditContext` and `SoftDeleteDomainService` (core). Split into `AddPlatformServices()` in core and `RegisterApplicationServices()` in the product, the latter calling the former.

### C5. `Extensions/DatabaseProviderExtensions.cs`

References both concrete contexts. Make generic: `AddDatabaseProvider<TContext>()`, called twice from `Program.cs`.

### C6. `Interfaces/ISampleModule.cs` — **RESOLVED: core**

Initially classified as sample scaffolding. Reading `ModuleExtensions.MapEndpoints` shows otherwise — it is a production safety gate:

```csharp
var modulesToMap = app.Environment.IsProduction()
    ? registeredModules.Where(m => m is not ISampleModule)
    : registeredModules;
```

Any module marked with it is excluded from endpoint mapping when the environment is Production. That is a general mechanism every product wants for demo and debug endpoints, and it is unrelated to OpenAPI tooling. It moves to core with `IModule` and `IRootModule`.

The name is misleading for what it does — it gates on environment, not on sample status. Renaming it (for example to `INonProductionModule`) is optional and out of scope here.

### C7. `Extensions/ModuleExtension.cs` — assembly scanning **BLOCKER**

Discovered while resolving C6. Module discovery scans the assembly that declares `IModule`:

```csharp
return typeof(IModule).Assembly.GetTypes()
```

Once `IModule` moves to core, this scans the core assembly and finds zero product modules. It does not fail to compile — it silently maps no endpoints at runtime, which is the worst failure mode available.

Fix: make the scan target explicit — `RegisterModules(this IServiceCollection services, params Assembly[] assemblies)` — with the product `Program.cs` passing its own assembly. The static `registeredModules` cache and its re-registration path for tests must keep working.

This must be covered by a test that asserts a non-zero module count after registration, otherwise the same regression can return unnoticed.

## Naming — **DECIDED: `Anvil`**

The hard constraint is that the name must not contain the token `ForgeKit`, or the template renames it and defeats the purpose. `Anvil` satisfies that and continues the forge metaphor: the anvil is the unchanging base that product-specific work is shaped on.

The namespace is `Anvil` with no organisation prefix. Package ID and namespace need not match, so if the core is ever published (see Non-Goals) the package can carry a prefixed ID while the namespace stays short.

The abstract context introduced in C1 is named `PlatformDbContext` rather than `AnvilDbContext`, because the name describes its role in the hierarchy rather than the assembly it lives in.

## Non-Goals

- Publishing the core as a NuGet package. This change is a prerequisite for that, not a substitute; the boundary must be proven in practice first.
- Changing the frontend. `app/` renames only 7 files with no directory renames, so it already merges cleanly from upstream.
- Removing the sample domain. Samples stay where they are and remain deletable per `docs/SAMPLES.md`.

## Risks

- **Migration drift.** The three migration projects target `AppDbContext`. Moving base configuration into `PlatformDbContext` must not alter the generated model. Verified by regenerating a migration and asserting an empty diff.
- **Boundary erosion.** Nothing mechanically prevents a future core file from importing a product type. Mitigated by the compile-time check that core builds without a reference to the product project.
- **The boundary will move.** Some files classified as product today are generic enough to promote later. That is expected; the classification is a starting position, not a permanent contract.

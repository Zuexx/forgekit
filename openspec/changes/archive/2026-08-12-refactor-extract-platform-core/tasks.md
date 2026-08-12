# Tasks: Extract Product-Neutral Platform Core

## 0. Decisions (blocking)

- [x] 0.1 Confirm the core project name — decided: `Anvil`
- [x] 0.2 Choose the `IUnitOfWork` decoupling option — decided: (b) generic `IUnitOfWork<TContext>`
- [x] 0.3 Resolve the `ISampleModule` disposition — resolved by code inspection: it is a production gate, moves to core

## 1. Create the core project

- [x] 1.1 Add `api/Anvil/Anvil.csproj` targeting `net10.0` with the package references the moved files require
- [x] 1.2 Add the project to `api/ForgeKit.sln`
- [x] 1.3 Reference the core project from `ForgeKit.Api`
- [x] 1.4 Confirm the core project has no reference to `ForgeKit.Api`

## 2. Move Layer A (41 files)

- [x] 2.1 Move the files listed in design.md Layer A with `git mv`, one area per commit
- [x] 2.2 Update namespaces to the core root namespace
- [x] 2.3 Update `using` directives across the product project
- [x] 2.4 Build after each area to keep failures localised

## 3. Split Layer C (7 files)

- [x] 3.1 Extract `PlatformDbContext` with soft-delete filters and camelCase naming; leave DbSets, relationships, and indexes in the product `AppDbContext`
- [x] 3.2 Keep `ConfigureJsonColumns` entirely in the product — on inspection the decimal-precision block is specific to `WorkspaceAnalytics.AverageCompletionDays`, not generic as the design assumed
- [x] 3.3 Apply the chosen `IUnitOfWork` decoupling and update `UnitOfWork`
- [x] 3.4 Split `ServiceExtension` into `AddPlatformServices()` and `RegisterApplicationServices()`
- [x] 3.5 Make `DatabaseProviderExtensions` generic over the context type
- [x] 3.6 Move `ISampleModule` to core with `IModule` and `IRootModule`
- [x] 3.7 Make module discovery scan explicit assemblies instead of `typeof(IModule).Assembly`
- [x] 3.8 Add a test asserting a non-zero registered module count

## 4. Verify no model drift

- [x] 4.1 Build and run the full test suite (baseline: 176 passed, 3 skipped)
- [x] 4.2 Regenerate a migration for each provider and confirm an empty diff against the current model
- [x] 4.3 Confirm existing migration identifiers are unchanged

## 5. Update the template

- [x] 5.1 Confirm `sourceName` does not rename the core project directory, assembly, or namespace
- [x] 5.2 Add the core project GUID to the `guids` list in `.template.config/template.json`
- [x] 5.3 Generate a project, build it, and run its tests
- [x] 5.4 Confirm the generated core path is identical to the base

## 6. Prove the merge benefit

- [x] 6.1 Generate a project, commit it, add the base as `upstream`
- [x] 6.2 Make a core-only change in the base and merge it downstream
- [x] 6.3 Confirm the shared layer syncs with no conflict; a whole-repo merge still duplicates the renamed product layer, so the documented workflow is a path-scoped sync

## 7. Documentation

- [x] 7.1 Update `docs/STRUCTURE.md` with the two-layer boundary
- [x] 7.2 Add the upstream merge workflow to `docs/FORKING_GUIDE.md`
- [x] 7.3 Record the layer boundary rule as an ADR
- [x] 7.4 Run `openspec validate refactor-extract-platform-core --strict --no-interactive`

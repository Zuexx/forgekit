# Change: Extract Product-Neutral Platform Core

## Why

ForgeKit is used as a development base: each product is generated from it and then diverges. Today the template renames `ForgeKit.Api` to the product name, which renames the project directory and therefore changes the path of all 135 API files. When the base gains a feature, downstream products cannot merge it — `git merge upstream/main` sees every shared file as a rename plus a content change, and rename detection degrades into large conflicts.

The dependency graph shows this cost is avoidable. Of 64 API source files, 41 contain no product-specific type at all, and 17 are unambiguously product code. Only 7 files couple the two layers, and each couples through a single seam.

Extracting the 41 product-neutral files into a project whose name never changes makes upstream merges path-identical for two thirds of the codebase, while product code keeps its own namespace.

## What Changes

- Add an `Anvil` project holding the product-neutral layer; its directory, assembly name, and root namespace are excluded from template renaming and are byte-identical in every generated product.
- Keep `<Product>.Api` as the composition root holding product entities, modules, services, and `Program.cs`.
- Split `AppDbContext` into an abstract `PlatformDbContext` (soft-delete filters, camelCase naming convention) in the core and a product `AppDbContext` that declares DbSets and relationships.
- Decouple `IUnitOfWork` from the concrete `AppDbContext` type so the interface can live in the core.
- Split `ServiceExtension` into platform registrations and product registrations.
- Make `DatabaseProviderExtensions` generic over the DbContext type.
- Update the `dotnet new` template so `sourceName` no longer touches the core project.
- Update structure and forking documentation to describe the two-layer boundary and the upstream merge workflow.

## Impact

- Affected specs: `platform-core` (new), `unit-of-work`
- Affected code: `api/ForgeKit.sln`, all 64 files under `api/ForgeKit.Api/`, the three migration projects, `api/ForgeKit.Api.Tests/`, `.template.config/template.json`, `docs/STRUCTURE.md`, `docs/FORKING_GUIDE.md`
- Database: no schema change; migration identifiers and the generated model must remain byte-identical
- Breaking changes: `IUnitOfWork.DbContext` changes its declared type; all `using ForgeKit.Api.*` imports for moved types change to the core namespace
- Not included: publishing the core as a NuGet package, and any change to the frontend, which renames only 7 files and already merges cleanly

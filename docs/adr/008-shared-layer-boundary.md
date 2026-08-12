# ADR-008: Shared Layer Boundary

**Status:** Accepted
**Date:** 2026-08-12
**Author:** ForgeKit Architecture Team

## Context

A starter kit that is used as a development base has two goals that pull against each
other. Each product wants its own namespace, so the scaffolding tool renames the project
and its directories. But renaming directories is what prevents the base from ever
delivering an improvement back: an upstream commit touching a shared file arrives on a
path that no longer exists downstream, and rename detection degrades into whole-file
conflicts across the codebase.

Teams usually resolve this by picking a side. Either the base is copied once and then
forked forever, and every improvement is re-implemented per product; or nothing is
renamed, and every product carries the starter kit's identity in its namespaces.

## Decision

Split the codebase by whether a file would be byte-identical in every product, and rename
only the half that would not.

The shared half lives in a project whose name the scaffolding tool does not touch. Its
paths and contents match the base exactly in every generated product, so a shared-layer
update is applied by path rather than merged:

```
git checkout upstream/main -- <shared-path>
```

The product half keeps the product's own name and namespace.

Membership in the shared layer is decided mechanically, not by taste: a file is shared
only if its transitive import closure contains no product type. The rule is enforced at
compile time by keeping the shared project free of any reference to the product project.

When a shared file appears to need a product type, the dependency is inverted through an
interface owned by the shared layer rather than moving the file out. The shared layer owns
the mechanism; the product owns the data and the concrete types.

## Consequences

**Positive**

- Shared-layer improvements reach existing products without re-implementation.
- The compile-time check makes boundary erosion a build failure rather than a slow drift.
- The split is a prerequisite for publishing the shared layer as a package later, without
  further restructuring.

**Negative**

- Contracts that referenced a product type must be generalised. Making the Unit of Work
  generic over its context added a type argument to every declaration site.
- The boundary is a judgement that will move. Files classified as product-specific may
  become general enough to promote, and each promotion is a small migration for every
  product already generated.
- A whole-repository merge from the base still does not work, because the product layer is
  still renamed. Products must sync by path, which is a workflow that has to be documented
  and remembered.

**Neutral**

- Two projects instead of one. The shared project needs its own package references and,
  outside the Web SDK, explicit global usings.

## When NOT to Use

Avoid this split when the base is copied once and genuinely never updated again, when only
one product will ever exist, or when the shared portion is small enough that a package
dependency is simpler than a source-level split.

## ForgeKit Implementation

`api/Anvil/` is the shared layer and `api/<Product>.Api/` the product layer. Of 67 API
source files, 48 are shared. `PlatformDbContext` holds the soft-delete filters, naming
convention, and audit stamping; the product `AppDbContext` derives from it and declares
DbSets, relationships, and indexes.

Two dependency inversions were needed. `IDataSeeder` lets the shared layer own when
seeding runs while the product owns what is written. Module discovery takes explicit
assemblies, because scanning the assembly that declares `IModule` would find only the
shared layer's modules and silently map no product endpoints.

## References

- `docs/STRUCTURE.md` — the layer table and boundary rule
- `docs/FORKING_GUIDE.md` — the upstream sync workflow
- `openspec/changes/archive/*-refactor-extract-platform-core/` — the change that introduced this

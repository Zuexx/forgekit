# Agent Instructions

ForgeKit is a full-stack starter kit used as a development base. Products are generated
from it with `dotnet new forgekit` and then diverge, so changes should stay reusable across
products rather than being tailored to the sample TODO domain.

## Read before changing `api/`

The API is split into two layers, and the split is load-bearing:

- **`api/Anvil/`** — the shared layer. The template never renames it, so its paths are
  identical in the base and in every generated product, which is what lets products pull in
  base improvements with `git checkout upstream/main -- api/Anvil api/Anvil.Tests`. It **must not**
  reference the product project; that is enforced by the build.
- **`api/ForgeKit.Api/`** — the product layer, renamed per product.

A file belongs in Anvil only if its transitive import closure contains no product type. If
a shared file appears to need a product type, invert the dependency through an interface
owned by Anvil (see `IDataSeeder`) rather than moving the file into the product layer.

Rationale and consequences: `docs/adr/008-shared-layer-boundary.md`, and the `platform-core`
specification.

## Planning changes

Specifications live in `openspec/`. Project context for planning is in
`openspec/config.yaml`, and the OpenSpec skills provide the workflow — use `/opsx:explore`
to think a change through, `/opsx:propose` to create one, `/opsx:apply` to implement, and
`/opsx:archive` when it ships.

Write a change proposal for new capabilities, breaking changes, architecture shifts, and
security work. Skip it for bug fixes, typos, dependency bumps, and configuration changes.

## Building and testing

```bash
cd api
dotnet build
dotnet test          # 193 tests across two projects; 3 skipped by design

cd ../app
pnpm install
pnpm check           # tsc --noEmit
pnpm lint
pnpm test            # vitest
pnpm test:e2e        # playwright; needs postgres and a built app
```

End-to-end tests run against a real database. Start one with `podman compose up -d` (or
docker), point `DATABASE_URL` at it and set `PGUSER=postgres`, create the auth schema with `pnpm auth.migration`,
then `pnpm build` before `pnpm test:e2e`.

`scripts/verify.sh` runs the full local gate. CI runs the same categories on every pull
request, plus migration drift checks and secret scanning.

Frontend unit tests cover `proxies/` — the authorization policy and request-context
resolution — and the auth config. End-to-end tests cover sign-in through to the database.
There is no component-level coverage, so UI details still need manual verification.

Sign-up is not implemented: the form's submit handler logs to the console, and there is no
`useSignUp` hook or `/signUp` route. The end-to-end test for it is marked `fixme` so the
gap stays visible.

## Conventions that are easy to get wrong

- **Migrations are provider-specific.** When the EF model changes, add migrations for all
  three provider projects. Never reuse one provider's migrations for another. CI fails on
  pending model changes.
- **Soft delete is automatic.** Entities implementing `ISoftDelete` get a global query
  filter from `PlatformDbContext`; do not add filters per entity.
- **Module discovery needs an explicit assembly.** `RegisterModules` scans the assemblies
  it is given plus Anvil. A product that forgets to pass its own assembly still builds and
  silently maps no endpoints.
- **Commits follow Conventional Commits** (`docs/api/COMMIT_CONVENTION.md`). Branch off
  main and open a PR so CI runs before merging.
- **Never commit local secrets.** `appsettings.Local.json` and `.env.local` are ignored and
  have committed `.example` counterparts.

## Where things are documented

| Topic | File |
|---|---|
| Project layout and the layer boundary | `docs/STRUCTURE.md` |
| Generating a product, syncing base updates | `docs/FORKING_GUIDE.md` |
| Architecture decisions | `docs/adr/` |
| Configuration and database providers | `docs/api/CONFIGURATION_GUIDE.md` |
| Structured logging patterns | `docs/api/logging.md` |
| Local setup | `docs/LOCAL_DEVELOPMENT.md` |

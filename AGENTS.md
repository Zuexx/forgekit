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

Specifications live in `openspec/`. Project context, artifact rules, and per-operation
guidance are in `openspec/config.yaml`, and OpenSpec delivers them at the step they apply
to — read what it hands you rather than working from memory of this file.

`/opsx:explore` to think a change through, `/opsx:propose` to create one, `/opsx:apply` to
implement, `/opsx:archive` when it ships.

Write a change proposal for new capabilities, breaking changes, architecture shifts, and
security work. Skip it for bug fixes, typos, dependency bumps, and configuration changes.

### Two loops, and who owns which

OpenSpec decides **what may be built and whether it counts as done**. Superpowers decides
**how it gets built and whether it was built correctly**. Neither knows the other exists,
so the seam is the `apply` and `archive` guidance in `openspec/config.yaml`.

Feature-level tasks live in `openspec/changes/<slug>/tasks.md`. Minute-level steps live in
the Superpowers plan, which cites those task ids under `## OpenSpec Coverage`. Keep the two
granularities apart; collapsing them makes the citation meaningless.

### Overlapping skills, resolved by trigger

Several skills cover the same ground. They differ in what fires them, which is what decides
between them:

| Job | Use | Because |
|---|---|---|
| Clarify a vague request | `superpowers:brainstorming` | Fires on its own before creative work |
| Stress-test a plan you already have | `grilling` | Only when you ask for it |
| Test-first implementation | `superpowers:test-driven-development` | The inner loop already speaks its vocabulary |
| Review inside the loop | `superpowers:requesting-code-review` | Dispatched per task by the loop itself |
| Review outside the loop | `/code-review` | Ad-hoc, on a diff you name |

Reach for `codegraph_explore` before reading files to answer "what does this affect" — it
returns the callers and the test-coverage gaps that reading cannot, in one call.

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

`.githooks/pre-push` checks that implementation plans cite OpenSpec task ids that
actually resolve — the one link between the two systems that nothing else validates.
Enable it once per clone with `git config core.hooksPath .githooks`.

`scripts/verify.sh` runs the full local gate. CI runs the same categories on every pull
request, plus migration drift checks and secret scanning.

Frontend unit tests cover `proxies/` — the authorization policy and request-context
resolution — and the auth config. End-to-end tests cover sign-in through to the database.
There is no component-level coverage, so UI details still need manual verification.

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

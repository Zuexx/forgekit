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

When a request is too vague for a proposal to state what it includes and excludes, run
`grillme` first — `pnpm exec grillme` from the repository root. It opens a browser, asks one
decision question at a time, and writes a Markdown handoff; it implements nothing. That handoff
is the input to `/opsx:propose`. It is the one gate a human closes: scope gets settled before
any agent receives the task, which is the boundary the rest of this file exists to hold.

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
| Test-first implementation | `superpowers:test-driven-development` | The inner loop already speaks its vocabulary |
| Execute a plan | `superpowers:subagent-driven-development`, **if its decision tree sends you there** | It routes tightly coupled tasks to manual execution; a task and its own verification are not independent |
| Review a change | `superpowers:requesting-code-review` | Whichever route the work took, including small changes done inline |
| Review an arbitrary diff | `/code-review` | Ad-hoc, outside a change |

Reach for `codegraph_explore` before reading files to answer "what does this affect" — it
returns the callers and the test-coverage gaps that reading cannot, in one call. It indexes
symbols, so a contract addressed by string — a header, an event name, a DI token, a config
key — is invisible to it and needs a literal search of its own.

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
Enabling it takes two steps, not one: `dotnet new` does not carry the executable bit, and git
ignores a hook it cannot execute without reporting anything.

```bash
git config core.hooksPath .githooks
chmod +x .githooks/*
```

`pnpm verify` runs the full local gate. CI runs the same categories on every pull request, plus
migration drift checks and secret scanning.

`pnpm preflight` reports whether this workflow is operational here at all — the declared tools,
whether `openspec/config.yaml` still yields its rules through the installed version, whether the
CodeGraph index reflects current source, whether the hooks can fire, and whether every capability
this file and `openspec/config.yaml` name still resolves. Run it after cloning, and when
something in the workflow behaves as though a piece is missing. Each failure names its fix.

Invoke both through pnpm rather than as `./scripts/*.sh` — in a generated product those files
arrive without the executable bit.

Frontend unit tests cover `proxies/` — the authorization policy and request-context
resolution — and the auth config. End-to-end tests cover sign-in through to the database.
There is no component-level coverage, so UI details still need manual verification.

## The shared workflow

`scripts/preflight.sh`, `scripts/sync-workflow.sh`, `.githooks/pre-push`, `.mcp.json`,
`.claude/settings.json`, `openspec/rules.yaml`, and `openspec/specs/workflow-toolchain/spec.md`
are owned by the [forgekit-workflow](https://github.com/Zuexx/forgekit-workflow) repository and
shared with every ForgeKit-family repo, including the iOS and Android starters. Edit them there,
not here:

```bash
pnpm sync-workflow && pnpm preflight
```

overwrites them and re-splices the shared rules into `openspec/config.yaml` below the marker
line, so a local edit disappears without a word. What this repository owns is everything above
that marker — its `context:` block — plus `scripts/verify.sh`, `package.json`, and this file.

**The workflow's own specification is now among the shared files.** Its twelve requirements
mention no stack, and they govern the iOS and Android starters as much as this repository, so
they are delivered to each rather than kept here — the repository bound by a requirement should
be the one that can read it. A change to what the workflow must *do* is therefore proposed and
archived in forgekit-workflow; an archive written here would be discarded by the next sync.
Specs describing this stack are unaffected and stay here.

Because `preflight.sh` is now shared verbatim, the stack half of it is declared in
`package.json` rather than written into the script:

```jsonc
"forgekit": {
  "sourceGlobs":     ["*.cs", "*.ts", "*.tsx", "..."],  // what counts as source
  "requiredTools":   ["dotnet"],                        // machine-level tools
  "nodeSubprojects": ["app"]                            // nested npm projects
}
```

A generated product inherits all of this. It has no `workflow` remote until one is added, and
`pnpm sync-workflow` says so rather than failing obscurely.

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

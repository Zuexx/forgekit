## Context

See proposal.md — Why. The constraints that shape the approach:

- The repository has no root `package.json`. `app/` is a pnpm project; `api/` is .NET. There is
  currently no home for tooling that belongs to neither.
- Claude Code reads configuration from three layers — user (`~/.claude/`), project
  (`.claude/settings.json`, `.mcp.json`), local (`.claude/settings.local.json`, gitignored).
  Only the project layer is version-controlled, and it is the layer this change targets.
- `.template.config/template.json` excludes nothing matching `.claude` or `mcp`, so files added
  at those paths propagate to generated products without touching the template's exclusion list.
- The workflow's rules are already delivered mechanically through `openspec instructions`. This
  change adds no rules; it makes the tools those rules name resolvable.

## Goals / Non-Goals

**Goals:**

- A clone is operational after one install command, with no per-tool setup.
- Failure to be operational is reported by a command, not discovered mid-task.
- Version drift is bounded by policy rather than by whichever version a machine happens to hold.

**Non-Goals:**

- Restructuring `app/` into a workspace. The root package exists for tooling only; `app/` keeps
  its own `package.json` and lockfile untouched.
- Vendoring Superpowers' skill files into the repository. The plugin is declared, not copied —
  copying would fork it and strand it at one version.
- Making `preflight.sh` block anything. It is added and runnable; where it is wired in is a
  separate decision (see proposal.md — Not included).

## Decisions

### A root `package.json` for tooling, not a workspace

The three CLIs belong to the repository, not to the frontend. Adding them to
`app/package.json` would ship workflow tooling inside the product's frontend dependency tree,
where they would be installed by anyone building the app and would appear in its audit surface.

A pnpm workspace spanning root and `app/` was considered and rejected: it would move `app/`'s
dependency resolution into a root lockfile, changing how the product's frontend installs, for
no benefit to this change. A standalone root package with its own lockfile keeps the blast
radius at zero.

`pnpm` is used at the root to match `app/`, so a developer needs one package manager rather
than two.

### Reference the local binary path, not `npx`

`.mcp.json` invokes CodeGraph through `./node_modules/.bin/codegraph` rather than
`npx codegraph`.

`npx` falls back to fetching from the registry when a binary is not found locally. That
fallback would silently defeat the entire version policy: on a machine where install has not
run, `npx codegraph` would download some version and appear to work, and the failure this
change exists to surface would be hidden again. An explicit path fails loudly, and preflight
names the fix.

### Version policy differs by tool, because the risk differs

| Tool | Declaration | Reasoning |
|---|---|---|
| `@fission-ai/openspec` | `^1.9.0` | The `rules.*` / `operations.*.guidance` config format is the seam the whole two-loop workflow rests on. Minors are accepted; a major could change that format and must be a decision. |
| `@colbymchenry/codegraph` | `^1.5.0` | Consumed through the MCP protocol and a CLI, both stable across minors. Same reasoning, lower stakes. |
| `grillme` | `latest` | Published at `0.1.2`. Under semver, `0.x` grants no compatibility promise, so a caret range would express a guarantee that does not exist. It sits upstream of the workflow and produces a Markdown file — breakage is visible immediately and contained. Tracking `latest` is honest about the situation; the lockfile provides reproducibility. |

Alternative considered: pin all three exactly. Rejected — the repository would ship a version
that is stale on the day it is generated, which is the failure the change is correcting.
`@fission-ai/openspec` is already at `1.9.0` while this machine holds `1.8.0`.

Reproducibility comes from the committed lockfile; the ranges describe what an intentional
update is permitted to pick up.

### Preflight verifies behaviour, not versions

The check that matters is not "is OpenSpec 1.x installed" but "does this project's
`config.yaml` still produce its rules through the installed tool". Those are different claims,
and this repository has already seen them diverge: a YAML plain scalar containing `: ` zeroed
the `rules` block while the file still parsed and `openspec validate` still passed.

So preflight reads values back and counts them:

```
openspec instructions proposal --change <any> --json   → rules present
openspec instructions apply    --change <any> --json   → guidance present
```

A count of zero is a failure regardless of the version string. This makes the check robust to
version changes it does not need to care about, and sensitive to the failure that actually
occurred here.

### Resolving cited capabilities

Preflight extracts backticked capability literals from `AGENTS.md` and `openspec/config.yaml`
and resolves each:

- `superpowers:<skill>` → a directory under the Superpowers plugin's `skills/`, located by glob
  rather than by hardcoded version, since the cache path embeds the plugin version
- `/opsx:<command>` → `.claude/commands/opsx/<command>.md`
- `grillme` → the declared binary
- `codegraph_explore` → the MCP server declared in `.mcp.json`

If the plugin root does not exist at all, that is reported as one failure ("Superpowers not
installed") rather than as one failure per skill, so the output names the cause instead of its
symptoms.

### Repositioning grillme in AGENTS.md

The current entry — `grilling`, under "Overlapping skills, resolved by trigger" — is wrong in
name and in kind. Grillme is a CLI that starts a local server, opens a browser, asks one
decision question at a time, and writes a Markdown handoff; its own README states it does not
implement the plan during the interview.

That places it upstream of `/opsx:propose`, not beside the skills it is currently listed with:
scope is settled by the human before any agent receives the task, which is the boundary the
workflow exists to hold. It moves out of the overlap table and into the planning section, with
its invocation stated.

## Risks / Trade-offs

- **A second lockfile at the root** → It covers three devDependencies with no runtime code, and
  `app/`'s install path is unchanged. The alternative — a workspace — costs more than it saves.
- **Declaring a plugin and an MCP server in tracked settings prompts each developer for
  approval on first open** → This is the intended behaviour, not a cost. The prompt is how a
  clone learns the repository expects those capabilities.
- **`grillme` at `latest` can pick up a breaking `0.x` release** → Contained: it runs before the
  workflow rather than inside it, its output is a Markdown file, and preflight reports it when
  it stops resolving. The alternative — a caret range on `0.x` — would imply protection semver
  does not provide.
- **Preflight's capability check depends on Claude Code's plugin cache layout, which is not a
  published contract** → Mitigated by globbing the version segment and by reporting a missing
  plugin root as a single named failure. If the layout changes, preflight fails loudly with a
  message pointing at itself, rather than passing silently — the correct direction to fail.
- **Index staleness is measured by mtime, which is a proxy** → A file touched without being
  changed reads as newer, and deleting a source file changes no mtime at all, so a deletion
  alone does not mark the index stale. Accepted: the check exists to catch the common case,
  an index left behind by ordinary editing.

## Decisions taken during implementation

These were settled while building, not while designing, and are recorded here because this is
where the next reader looks for the reasoning.

### Index staleness is measured against source, not against the commit

The original design compared the index against `HEAD`'s commit time and accepted that it would
"report stale during ordinary work". Implementation showed that was wrong in kind, not degree:
committing modifies no source file, so the check went red after **every commit** while the index
was perfectly current. A check that is red for a reason unrelated to what it guards trains its
readers to skip it, which costs more than the check was worth. It now compares the index against
the newest tracked source file.

### Build scripts are declined through `allowBuilds`

pnpm 11 exits non-zero — not merely warns — when a dependency's build script is skipped without
a decision, which would break the install this change promises. The recognised setting is
`allowBuilds` in `pnpm-workspace.yaml`; `onlyBuiltDependencies`, `ignoredBuiltDependencies`, and
`strictDepBuilds` are not read in this version, from `package.json` or `.npmrc`. Both skipped
scripts were inspected and neither is needed: OpenSpec's `postinstall` prints an opt-in
completion hint, and `msgpackr-extract` is a native accelerator with a pure-JS fallback.

### The capability check reads every table cell, and document paths too

Fixing on one table column skips a capability cited in another — the same "passed because it
never looked" hole the check exists to close, moved one axis over. It reads every cell, and an
unrecognised token fails rather than being ignored, because the defect that motivated the check
(`grilling`) carries no prefix a pattern could match. Document paths are resolved on the same
grounds: a pointer at a deleted file rots identically to a stale skill name.

### Scripts are invoked through pnpm, hooks are reported

`dotnet new` does not carry the executable bit, so in a generated product every script and hook
arrives non-executable. Scripts sidestep this through `pnpm preflight` / `pnpm verify`, which do
not depend on file modes. A git hook cannot: git requires the bit and ignores a hook without it
in silence. Preflight therefore reports an unexecutable hook rather than letting the gate fail
quietly — which it had been doing in every generated product.

## Migration Plan

No runtime migration. Existing clones adopt the change by running the root install once and
enabling hooks if not already enabled; global installs may remain and are simply no longer
depended upon. Rollback is deleting the added files — nothing else references them until
preflight is wired into a gate, which this change does not do.

## Why

The AI development workflow's rules live in this repository — `openspec/config.yaml`,
`AGENTS.md`, `.githooks/pre-push`, 16 specs — but the tools that execute those rules live on
whichever machine happens to be configured: OpenSpec and CodeGraph as global npm installs, the
CodeGraph MCP server in `~/.claude.json`, Superpowers in `~/.claude/settings.json`. Clone the
repo on a second machine, or generate a product from the template, and the rules arrive intact
while the capacity to follow them does not. Nothing reports the mismatch.

This is not hypothetical. `AGENTS.md` already names a `grilling` capability that resolves
nowhere on the machine this proposal was written on, and the reference has been sitting there
unnoticed. A gate whose enforcement is ambient is a gate that quietly stops existing.

## What Changes

- Add a root `package.json` declaring the workflow CLIs as devDependencies, so `npm i` provides
  them at known-compatible versions instead of relying on global installs.
- Add `.mcp.json` at the repository root declaring the CodeGraph MCP server, moving it from
  user scope into the repository.
- Add a tracked `.claude/settings.json` declaring the Superpowers plugin, so the skills named
  by `openspec/config.yaml` resolve wherever the repo is cloned.
- Install `grillme` as a required dependency and correct `AGENTS.md`, which currently names it
  `grilling` and classifies it as a skill. It is a CLI that runs an interactive interview in a
  browser and writes a Markdown handoff; it belongs upstream of `/opsx:propose`, not beside it.
- Add `scripts/preflight.sh`, which verifies the workflow is operational — every cited skill
  resolves, `openspec` reads this project's config back, the CodeGraph index exists and is not
  older than `HEAD`, `core.hooksPath` points at `.githooks`.
- Record the version policy for the workflow CLIs in `docs/DEPENDENCY_CONSTRAINTS.md`.
- Remove the `.mem0-src/` entries from `.gitignore` and `.template.config/template.json`. They
  are residue from a superseded design; the memory layer is the repository itself.

## Capabilities

### New Capabilities

- `workflow-toolchain`: what the repository must declare about the tools its documented
  workflow depends on, and how a clone verifies those tools are present and usable before
  relying on them.

### Modified Capabilities

None. The existing specs describe the running application; none of them states a requirement
about the development toolchain.

## Impact

**`codegraph_explore` has nothing to contribute to this change, and saying so is the honest
form of rule 1.** The graph indexes symbols. This change adds no symbols and modifies no
function: every artifact it touches is configuration, documentation, or a shell script. Its
entire surface is the string-addressed kind rule 4 describes — config keys, package names,
skill identifiers, file paths — so the evidence below comes from literal searches, run for this
proposal rather than recalled.

**Every capability named by `AGENTS.md` and `openspec/config.yaml`, resolved against this
machine.** Thirteen literals were extracted from those two files and each was checked against
`~/.claude/plugins/cache/claude-plugins-official/superpowers/6.2.0/skills/` and
`.claude/commands/opsx/`:

- 6 `superpowers:*` skills — all resolve
- 4 `/opsx:*` commands — all resolve
- `grilling` — **does not resolve.** Not among the 14 skills Superpowers 6.2.0 ships, and not
  in any installed plugin.

`codegraph_explore` and `/code-review` were excluded from the count: the first is an MCP tool
whose availability this change addresses directly, the second is a built-in.

**Package identities and current versions, from `npm view`:**

- `@fission-ai/openspec` — latest `1.9.0`; this machine has `1.8.0` globally installed
- `@colbymchenry/codegraph` — latest `1.5.0`
- `grillme` — latest `0.1.2`, `bin: { grillme }`, requires Node 22.16+

None publishes an `lts` dist-tag; `openspec` publishes `latest`, `next`, and `beta`, the other
two only `latest`. A version policy for these has to be expressed as a semver range, not as a
release track.

**Files touched.** `package.json`, `.mcp.json`, `.claude/settings.json`, and
`scripts/preflight.sh` do not currently exist — all four are new. `.gitignore`,
`.template.config/template.json`, `AGENTS.md`, and `docs/DEPENDENCY_CONSTRAINTS.md` are edits.

**Template propagation, verified by literal search.** `.template.config/template.json` contains
zero occurrences of `.claude` or `mcp`, so nothing in its exclusion list stops the new files
from reaching a generated product. `.gitignore:72` ignores `.claude/settings.local.json` only,
leaving a tracked `.claude/settings.json` free to be committed.

**What breaks if this is wrong.** The dependency that carries real risk is the OpenSpec version
range. The `rules.*` and `operations.*.guidance` blocks the whole two-loop seam rests on are a
1.x config format, and this repository has already been bitten once by that file failing
silently — a YAML plain scalar containing `: ` zeroed the entire `rules` block while
`openspec validate` still passed, because it does not validate config. A major version that
changed the format would reproduce exactly that failure mode. This is why `preflight.sh` must
read the rules back out of `openspec instructions` and count them rather than compare a version
string: the version being acceptable and the config being readable are different claims.

**Not included in this change:**

- No CI job. `preflight.sh` is added and made runnable; wiring it into `.github/workflows/ci.yml`
  or into `scripts/verify.sh` is a separate decision about what should block a merge.
- No automated dependency updates. Whether to adopt Dependabot or Renovate — neither is
  configured today — is deferred.
- No home or retention policy for the Markdown handoffs `grillme` writes into the repository.
  That needs the tool run once to see what it actually produces and where.
- No change to the workflow's rules. `openspec/config.yaml`'s guidance blocks are untouched;
  this change makes the tools those blocks name available, and does not revise what they say.
- No removal of the global installs on any developer machine. The repository stops depending on
  them; it does not require them gone.

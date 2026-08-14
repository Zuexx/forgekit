## Settled before implementation

These were decided in design.md and are not open for rediscovery mid-task:

- Root `package.json` for tooling only, installed with pnpm, its own lockfile. `app/` is not
  touched and does not become a workspace member.
- `@fission-ai/openspec` at `^1.9.0`, `@colbymchenry/codegraph` at `^1.5.0`, `grillme` at
  `latest`. The lockfile is committed.
- `.mcp.json` invokes `./node_modules/.bin/codegraph`, never `npx` — `npx` would fetch from the
  registry when the local binary is absent and hide the failure this change exists to surface.
- Preflight judges the workflow config by reading its values back and counting them, not by
  comparing version strings.
- `preflight.sh` is not wired into any gate by this change.

## 1. Toolchain declaration

- [ ] 1.1 Declare the three workflow CLIs in a new root `package.json` and commit the lockfile.
  Done when `pnpm install` at the repository root produces `node_modules/.bin/openspec`,
  `node_modules/.bin/codegraph`, and `node_modules/.bin/grillme`, each executing successfully
  with `--version` or its equivalent, on a machine where none is installed globally.

- [ ] 1.2 Confirm the root install does not disturb the frontend. Done when `app/`'s lockfile is
  unchanged by `git status`, and `scripts/verify.sh` still exits 0.

- [ ] 1.3 Make `scripts/verify.sh` resolve the declared binaries before global ones. It currently
  calls `require_command openspec`, which passes only where OpenSpec is installed globally —
  the dependency this change removes. Done when `verify.sh` exits 0 in a clone where `openspec`
  exists only under `node_modules/.bin`, and its OpenSpec step runs the declared version.

## 2. Capability declarations

- [ ] 2.1 Declare the CodeGraph MCP server in a new root `.mcp.json` pointing at the local
  binary. Done when a Claude Code session opened in a fresh clone offers the `codegraph` server
  for approval and `codegraph_explore` returns results after approval.

- [ ] 2.2 Declare the Superpowers plugin in a new tracked `.claude/settings.json`. Done when the
  six `superpowers:*` skills named by `AGENTS.md` and `openspec/config.yaml` resolve in a clone
  whose user-level settings do not enable the plugin.

## 3. Workflow preflight

- [ ] 3.1 Implement `scripts/preflight.sh` covering the four checks the spec requires — declared
  tools present, workflow config readable, code index present and not older than `HEAD`, git
  hooks enabled — each failure naming the command that resolves it. Done when it exits 0 on
  this repository as currently configured, and exits non-zero with the correct message for each
  check when that check's precondition is removed and restored.

- [ ] 3.2 Add the cited-capability resolution check, resolving `superpowers:*` by glob against
  the plugin cache, `/opsx:*` against `.claude/commands/opsx/`, `grillme` against the declared
  binary, and `codegraph_explore` against `.mcp.json`. Done when it reports every currently
  cited capability as resolving, and reports a single named failure — not one per skill — when
  the Superpowers plugin root is absent.

- [ ] 3.3 Verify the config-readback check catches a silently empty section. Done when
  temporarily breaking a `rules:` entry in `openspec/config.yaml` the way a plain scalar
  containing `: ` breaks it makes preflight fail, and restoring it makes preflight pass.

- [ ] 3.4 Make the repository's scripts runnable where file modes are not preserved, and report
  a hook that cannot fire. `dotnet new` does not carry the executable bit, so in every generated
  product `scripts/preflight.sh`, `scripts/verify.sh`, and `.githooks/pre-push` arrive as
  `rw-r--r--` — the first two fail with `permission denied`, and git ignores the third in
  silence, meaning the OpenSpec task-id gate has never fired in any generated product. Done when
  both scripts run in a generated product through a declared command without any permission
  change, and preflight fails with a naming message when a hook under `core.hooksPath` is not
  executable.

- [ ] 3.5 Stop `.githooks/pre-push` reading past the `## OpenSpec Coverage` section. It ends the
  section only at the next `## ` heading, so a plan using `### ` subheadings after it has its
  whole body scanned, and every `N.M` in the prose — version numbers like `^1.9.0` and `>=22.16`
  — is read as a task id. The result is a refused push with invented ids, which is the false
  positive that teaches people to bypass the gate. Done when the hook passes on a plan whose
  coverage section is followed by `### ` headings and version numbers, and still fails on a plan
  citing a task id that genuinely does not exist.

## 4. Instruction corrections

- [ ] 4.1 Correct grillme in `AGENTS.md`: remove the `grilling` row from the overlapping-skills
  table and describe it in the planning section as a CLI run before `/opsx:propose`, naming its
  invocation. Done when preflight's capability check passes with no unresolved literal, and no
  occurrence of `grilling` remains in `AGENTS.md`.

## 5. Version policy and residue

- [ ] 5.1 Record the workflow CLI version policy in `docs/DEPENDENCY_CONSTRAINTS.md`, following
  that file's existing constraint / reason / revisit format, including why `grillme` is treated
  differently from the other two. Done when the file states a policy for all three tools.

- [ ] 5.2 Remove the `.mem0-src/` entries from `.gitignore` and
  `.template.config/template.json`, and collapse the duplicated `.codegraph/` entry in
  `.gitignore`. Done when `grep -rn mem0` over tracked files returns nothing and `.codegraph/`
  appears once.

## 6. Propagation

- [ ] 6.1 Verify a generated product receives a working toolchain. Done when
  `dotnet new forgekit` output contains `package.json`, `.mcp.json`, `.claude/settings.json`,
  and `scripts/preflight.sh`, and `pnpm install && ./scripts/preflight.sh` in that generated
  project exits 0 after its hooks are enabled.

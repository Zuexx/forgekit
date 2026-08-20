# Dependency Constraints

This starter kit keeps most packages current, but a few major versions are intentionally held back until their surrounding ecosystem is compatible.

## Frontend

### ESLint

- Current constraint: keep `eslint` on 9.x.
- Reason: ESLint 10 currently breaks existing lint plugins used by the app, including React-related rules.
- Revisit when: `eslint-config-next`, `eslint-plugin-react`, `eslint-plugin-jsx-a11y`, and `eslint-plugin-import` all support ESLint 10 without peer or runtime failures.

### TypeScript

- Current constraint: keep `typescript` on 5.x.
- Reason: TypeScript 6 currently conflicts with the `i18next` peer dependency range.
- Revisit when: `i18next` supports TypeScript 6, or the project no longer depends on the constrained peer path.

### Tedious

- Current constraint: keep `tedious` on 19.x.
- Reason: `kysely-codegen` currently declares support for `tedious` versions below 20.
- Revisit when: `kysely-codegen` supports `tedious` 20, or the project replaces the SQL Server code generation path.

### Next.js Build

- Current constraint: use `next build --webpack`.
- Reason: Next.js 16 Turbopack build can require local process/socket behavior that is not stable in constrained local or CI environments. Webpack build passes the production verification path.
- Revisit when: Turbopack build passes reliably in the target developer and CI environments.

## Workflow Tooling

Declared in the root `package.json`, which exists only for the development workflow — these are
not part of the API or the app. None of the three publishes an LTS track, so the policy below is
expressed as a semver range rather than as a release channel.

### OpenSpec

- Current constraint: keep `@fission-ai/openspec` on 1.x (`^1.9.0`).
- Reason: `openspec/config.yaml`'s `rules.*` and `operations.*.guidance` blocks are a 1.x
  format, and they are the seam the two-loop workflow rests on. A major version could change
  that format, and the failure would be silent — `openspec validate` does not validate config.
- Revisit when: a 2.x is published and `scripts/preflight.sh` still reports the config's rules
  and guidance as readable against it.

### CodeGraph

- Current constraint: keep `@colbymchenry/codegraph` on 1.x (`^1.5.0`).
- Reason: consumed through the MCP protocol and a CLI, both stable across minors.
- Revisit when: a 2.x is published and `codegraph_explore` still returns results through the
  server declared in `.mcp.json`.

### Grillme

- Current constraint: track `latest`.
- Reason: published at 0.x, where semver grants no compatibility promise — a caret range would
  express a guarantee that does not exist. It runs upstream of the workflow and produces a
  Markdown file, so breakage is immediate and contained. The committed lockfile supplies
  reproducibility.
- Revisit when: it reaches 1.0, at which point a caret range becomes meaningful.

### Build scripts

- Current constraint: `@fission-ai/openspec` and `msgpackr-extract` are declined in
  `pnpm-workspace.yaml`'s `allowBuilds`.
- Reason: pnpm 11 exits non-zero when a build script is skipped without a decision. OpenSpec's
  `postinstall` only prints an opt-in shell-completion hint; `msgpackr-extract` is a native
  accelerator reached through `grillme → effect` that falls back to pure JS. Neither is needed.
- Revisit when: either package's install script starts doing work the CLI depends on.

### The shared workflow repository

- Current constraint: `scripts/preflight.sh`, `scripts/sync-workflow.sh`, `.githooks/pre-push`,
  `.mcp.json`, `.claude/settings.json`, `openspec/rules.yaml`, and
  `openspec/specs/workflow-toolchain/spec.md` are pulled from
  [forgekit-workflow](https://github.com/Zuexx/forgekit-workflow) and must not be edited here.
- Reason: the same files run in forgekit-ios and forgekit-android. An edit made here is
  overwritten by the next `pnpm sync-workflow` without a word, and until then the three
  repositories disagree about a workflow that is supposed to be one thing.
- Revisit when: a change genuinely applies to this stack only — in which case it belongs in
  `scripts/verify.sh` or in this repository's own `context:` block, not in a shared file.

## Keeping these current

`.github/dependabot.yml` opens weekly pull requests for the root workflow package, `app/`, and
`api/`, and monthly ones for the GitHub Actions themselves. The majors held back above are
excluded there by name, so minors and patches still arrive while the constraint stands. Removing
a constraint here means removing its `ignore` entry too — otherwise the constraint is lifted in
prose and still enforced by the bot.

## Verification

After revisiting any constraint, run:

```bash
cd app
pnpm peers check
pnpm check
pnpm lint
BETTER_AUTH_SECRET="$(openssl rand -base64 32)" pnpm build
```

**On a workflow-tooling bump, run `pnpm preflight` before merging it.** CI does not. A release
can satisfy its semver range and still stop `openspec/config.yaml` yielding its rules, which is
the failure preflight's config check exists to catch and the one nothing else would notice.

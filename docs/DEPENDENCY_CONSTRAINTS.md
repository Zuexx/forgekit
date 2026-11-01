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

### Node.js

- Current constraint: `app/` declares `engines.node` `^24.0.0` and `packageManager` `pnpm@11.1.1`,
  matching CI and the README.
- Reason: `better-sqlite3` (the default SQLite adapter) ships prebuilt binaries per Node ABI.
  On a Node version without a prebuild, `pnpm install` falls back to compiling it and fails
  on machines without a C++ toolchain. Node 24 is the version CI verifies.
- Revisit when: CI moves to the next Node LTS. Change `engines.node` here, `node-version` in
  `.github/workflows/ci.yml`, and the README prerequisites together.

### Better Auth CLI

- Current constraint: the CLI runs as `pnpm dlx auth@<version>`, where `<version>` is the
  `better-auth` version in `app/pnpm-lock.yaml`. It appears in `app/package.json`'s
  `auth.generate`/`auth.migration` scripts and in `.github/workflows/ci.yml`.
- Reason: the CLI pins an exact `better-auth` dependency and reads the schema from it, so a
  CLI from another release can generate or migrate a different schema than the app expects.
  The old `@better-auth/cli` package is deprecated and its `latest` tag still resolves to the
  1.4 line, which is why `@latest` silently drifted.
- Dependabot does not see a version inside a script string. Bump these three references in the
  same pull request that bumps `better-auth`.

## Backend

### Microsoft.OpenApi

- Current constraint: keep `Microsoft.OpenApi` inside the range `Microsoft.AspNetCore.OpenApi`
  declares (currently `>= 2.12.2 && < 3.0.0`; pinned at 2.x in `api/Directory.Packages.props`).
  A Dependabot PR that moves it to 3.x fails `OpenApiDocumentTests`; close it.
- Reason: the two packages are bound at runtime. An out-of-range version still compiles, then
  fails on the first request to `/openapi/v1.json` with a `MissingMethodException`, and the
  Scalar reference renders empty. That is what the earlier 3.x pin did, behind a NU1608 warning
  every build reported and a removed source generator. `OpenApiDocumentTests` now requests the
  document, so an incompatible pair fails CI.
- Revisit when: a `Microsoft.AspNetCore.OpenApi` release accepts `Microsoft.OpenApi` 3.x. Bump
  both in one PR and let `OpenApiDocumentTests` decide.

## AI Workflow

### pstack

- Current constraint: pinned to one commit of `cursor/plugins` by `sha` in
  `.claude/settings.json`.
- Reason: a plugin release changes how the agent behaves, so it should arrive as a reviewed
  pull request rather than on its own. Dependabot does not track it.
- Bump procedure: `docs/AI_WORKFLOW.md`.

## Keeping these current

`.github/dependabot.yml` opens monthly, grouped pull requests for `app/`, `api/`, and the GitHub
Actions themselves; they are merged by hand once CI passes. The majors held back above are
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

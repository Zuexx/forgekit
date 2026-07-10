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

## Verification

After revisiting any constraint, run:

```bash
cd app
pnpm peers check
pnpm check
pnpm lint
BETTER_AUTH_SECRET="$(openssl rand -base64 32)" pnpm build
```

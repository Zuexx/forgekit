## 1. Add the dependency

- [ ] 1.1 Add `server-only` to `app/package.json`'s dependencies and install it

## 2. Guard the server-only modules

- [ ] 2.1 Add `import "server-only"` as the first import in `app/lib/auth.config.ts`, with a
  short comment stating why (points at this change / the sibling kit's incident)
- [ ] 2.2 Add `import "server-only"` as the first import in `app/lib/db/sqlite.ts`
- [ ] 2.3 Add `import "server-only"` as the first import in `app/lib/db/postgres.ts`
- [ ] 2.4 Add `import "server-only"` as the first import in `app/lib/db/mssql.ts`

## 3. Verify

- [ ] 3.1 `cd app && pnpm build` succeeds unchanged (confirms every existing call site is
  genuinely server-only, per the design's Goals)
- [ ] 3.2 `cd app && pnpm check && pnpm lint && pnpm test` all pass
- [ ] 3.3 Temporarily add `import { auth } from "@/lib/auth.config"` to a "use client"
  component, run `pnpm build`, confirm it fails with a `server-only`-attributed error naming
  `auth.config.ts`, then revert the temporary import (this is a manual regression check, not
  a committed test — Next's own build step is the enforcement mechanism, matching the
  design's Non-Goals on not adding parallel tooling for this)

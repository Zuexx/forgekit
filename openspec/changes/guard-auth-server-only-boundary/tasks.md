## 1. Add the dependency

- [x] 1.1 Add `server-only` to `app/package.json`'s dependencies and install it

## 2. Guard the server-only modules

- [x] 2.1 Add `import "server-only"` as the first import in `app/lib/auth.config.ts`, with a
  short comment stating why (points at this change / the sibling kit's incident)
- [x] 2.2 Add `import "server-only"` as the first import in `app/lib/db/sqlite.ts`
- [x] 2.3 Add `import "server-only"` as the first import in `app/lib/db/postgres.ts`
- [x] 2.4 Add `import "server-only"` as the first import in `app/lib/db/mssql.ts`

## 3. Verify

- [x] 3.1 `cd app && pnpm build` succeeds unchanged (confirms every existing call site is
  genuinely server-only, per the design's Goals)
- [x] 3.2 `cd app && pnpm check && pnpm lint && pnpm test` all pass — **first run surfaced a
  real regression not anticipated in the design**: `lib/auth.config.test.ts` failed to even
  collect (`server-only`'s actual package body unconditionally throws), and
  `lib/db/sqlite.test.ts` failed the same way, because Vitest runs test files directly under
  Node rather than through Next's webpack build, so it has none of Next's special-cased
  resolution that swaps `server-only` for a no-op in a server compilation and only leaves the
  throw for a client one. Fixed by aliasing the `server-only` specifier to a local no-op stub
  (`app/vitest.server-only-stub.ts`) in `app/vitest.config.mts`'s `resolve.alias` — the
  standard, documented fix for this exact interaction. Re-ran clean after the fix: 0 type
  errors, 0 lint errors (1 pre-existing unrelated warning), 53/53 tests passing.
- [x] 3.3 Temporarily added `import { auth } from "@/lib/auth.config"` plus a use of it to
  `app/components/locale-switcher.tsx` (a real `"use client"` component), ran `pnpm build`:
  failed exactly as expected, with webpack's own error naming `./lib/auth.config.ts` and a
  full import trace (`auth.config.ts` → `locale-switcher.tsx` →
  `sign-in-card.tsx`) pointing at the real offending chain. Reverted the temporary import;
  `pnpm build` confirmed green again afterward.

## 4. Post-review fix

- [x] 4.1 A code review found real Important-but-not-blocking gaps in the Vitest alias from
  task 3.2: it means `pnpm test` alone can no longer catch a client-bundle leak of a guarded
  module (a blunt but real signal it gave, pre-alias). The review independently confirmed
  this doesn't weaken the actual guarantee the spec requires -- `pnpm build` (which does
  catch it) runs unconditionally right after `pnpm test` in `pnpm verify`/CI -- and that this
  suite has no component-level test that would have exercised the leak either way. Added a
  paragraph to `app/vitest.server-only-stub.ts`'s comment making this trade-off explicit
  instead of leaving it for a future reader to reconstruct. Re-ran `pnpm test`: 53/53 passing,
  unaffected by the comment-only change.

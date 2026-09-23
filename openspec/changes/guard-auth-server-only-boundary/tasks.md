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

## 5. CI failure: a third loader that can't resolve "server-only"

Opening the PR surfaced a real, blocking failure neither the design, the two rounds of local
verification, nor the code review caught, because none of them ran the one thing CI actually
runs that neither `pnpm build` nor `pnpm test` do: the better-auth CLI, invoked directly
against `lib/auth.config.ts`.

- [x] 5.1 CI's "Create auth schema" step
  (`pnpm dlx @better-auth/cli@latest migrate --config ./lib/auth.config.ts -y`) failed:
  `Please remove import 'server-only' from your auth config file temporarily. The CLI cannot
  resolve the configuration with it included.` The CLI loads its `--config` target with its
  own module loader, entirely outside both Next's webpack build (which has the special-cased
  resolution that makes `server-only` a no-op server-side) and Vitest (aliased in task 3.2) --
  a third environment this design never accounted for, and the one every migration in CI and
  every contributor running `pnpm auth.migration`/`pnpm auth.generate` actually depends on.
- [x] 5.2 Fixed by splitting every guarded file into two: an unguarded `*-instance.ts` holding
  the real implementation, and the original filename reduced to a two-line guarded wrapper
  (`import "server-only"; export * from "./*-instance"`). Applied consistently to all four
  guarded files from task 2, not just `auth.config.ts` -- `auth-instance.ts` (new) imports the
  three database adapters from their own new `*-instance.ts` files, not their guarded
  wrapper filenames, so loading it never transitively re-triggers a guard one level deeper
  (which a narrower fix touching only `auth.config.ts` would have hit immediately, since it
  still imports `lib/db/{sqlite,postgres,mssql}.ts`, each guarded since task 2). This keeps
  every guarantee task 2 established intact -- including the "defense in depth" reasoning in
  `design.md` for guarding the database adapters independently of `auth.config.ts` -- while
  giving the CLI (and only the CLI) an entry point with no guard anywhere in its import graph.
  Updated the CLI's three callers to the new unguarded path: `app/package.json`'s
  `auth.generate`/`auth.migration` scripts and `.github/workflows/ci.yml`'s "Create auth
  schema" step now point at `lib/auth-instance.ts` instead of `lib/auth.config.ts`.
- [x] 5.3 Verified the fix directly, not just inferred it: ran
  `pnpm dlx @better-auth/cli@latest migrate --config lib/auth-instance.ts` against a scratch
  SQLite database (not the dev database) -- completed successfully, no `server-only`
  resolution error, matching exactly what CI's step now does.
- [x] 5.4 Re-ran the full local verification after the restructure: `pnpm check`/`lint`/`test`
  all clean (53/53 tests), `pnpm build` succeeds. Re-ran task 3.3's regression check
  (temporarily importing `auth.config` from a real `"use client"` component) against the
  restructured files: the build still fails with the same webpack error naming
  `./lib/auth.config.ts` and the same import trace, confirming the guard itself is unaffected
  by the split. Reverted the temporary import; build green again.
- [x] 5.5 Confirmed no app code imports any of the four new `*-instance.ts` files directly
  (`grep -rn "auth-instance\|sqlite-instance\|postgres-instance\|mssql-instance"` across
  `app/`) -- each is reachable only through its own guarded wrapper, or (for `auth-instance.ts`
  and the two database instance files it imports) through the CLI's `--config` flag.

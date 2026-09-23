## Why

The sibling TanStack Start starter (`forgekit-tanstack-start`) shipped an app-wide
client-hydration break: a server-only auth module (dotenv, `node:path`, a database adapter,
all evaluated as import-time side effects) got pulled into the client bundle through an
ordinary-looking import, and every `onClick`/`onSubmit` silently stopped working — invisible
to every prior curl/jsdom-based check, since none of them executed real browser JS. It was
fixed there by moving the risky import behind a dynamic `import()` and renaming the file so
the framework's own build-time import-protection plugin would catch a future leak at build
time instead of letting it fail silently at runtime.

This kit's own `lib/auth.config.ts` (and the three database adapters it composes:
`lib/db/sqlite.ts`, `lib/db/postgres.ts`, `lib/db/mssql.ts`) has the exact same shape of
side-effecting, server-only module — and today, nothing would catch it if a future change
pulled one into a client bundle. No such leak exists right now (see Impact below), but the
guard against it does not exist either, which is exactly the state the sibling kit was in
right before its bug shipped.

## What Changes

- Add the `server-only` package (Vercel's own sentinel for this exact problem: importing it
  from a module that later gets pulled into a client bundle fails the Next.js build with a
  clear error, rather than shipping broken client JS) as a dependency of `app/`.
- Add `import "server-only"` as the first import in `lib/auth.config.ts`, `lib/db/sqlite.ts`,
  `lib/db/postgres.ts`, and `lib/db/mssql.ts` — the modules with dotenv/filesystem/database
  side effects at import time.
- No dynamic-import rewrite (unlike the sibling fix): Next.js's own Server
  Component/Route Handler model already keeps every current caller of these modules
  server-only by construction (see Impact); the sentinel is what is actually missing here,
  not a runtime workaround for a bundler that lacks Next's client/server split.

## Capabilities

### New Capabilities
(none)

### Modified Capabilities
- `authentication`: adds a build-time guarantee that the auth module and its database
  adapters cannot be included in a client bundle, closing the gap the sibling kit's incident
  exposed.

## Impact

Grounded in `codegraph_explore` over the `forgekit` project plus direct reads (barrel/import
graphs are not resolved by the symbol index, since they are string-keyed module paths, not
named symbols — checked separately and noted where relied on):

- `lib/index.ts` re-exports `lib/auth.config.ts` via `export * from "./auth.config"`,
  alongside `auth-client.ts`, `queries`, `rpc`, and `store`. A literal grep across `app/` for
  every import of the bare barrel (`from "@/lib"`, not a deeper path like `@/lib/queries`)
  found zero matches — every current consumer imports a specific deep path
  (`@/lib/queries`, `@/lib/store`), never the barrel. The barrel's re-export of
  `auth.config` is unused today, which is exactly why it is a *latent* risk rather than an
  active bug: nothing currently breaks, but nothing stops a future contributor reaching for
  `@/lib` out of habit and pulling the whole graph in, the same way the sibling kit's bug
  was introduced.
- `sessionMiddleware` (`app/lib/rpc/session-middleware.ts:14`, its only caller
  `app/features/authenticate/route.ts`) imports `auth` from `auth.config.ts` directly. It
  needs no guard of its own: it is itself only ever invoked from
  `app/app/api/[[...hono]]/route.ts`, a Next.js Route Handler, which Next never bundles for
  client execution regardless of what it imports. Guarding `auth.config.ts` protects this
  path transitively.
- `lib/db/mssql.ts`, `lib/db/postgres.ts`, and (by earlier direct read) `lib/db/sqlite.ts`
  each run `dotenv`'s `config()` against a filesystem path at module scope, then construct a
  live connection object (`Kysely`, a `pg.Pool`, or an MSSQL dialect) — the same class of
  side effect that broke client hydration in the sibling kit, just for three files instead of
  one call site. All three are already only reachable through `auth.config.ts`, which
  composes them; no other file imports any of the three directly.
- What breaks if this is wrong: the `server-only` sentinel changes nothing at runtime for any
  currently-passing path — it only adds a new build-time failure mode for an import path that
  does not exist today. The risk is a false positive (a legitimate server-only call site
  the sentinel doesn't expect), not a runtime regression; `pnpm build` after the change is
  the direct check for that.

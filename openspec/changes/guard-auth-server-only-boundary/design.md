## Context

See `proposal.md` - Why for the sibling incident that motivates this. The relevant current
state here: `lib/auth.config.ts` and its three database adapters
(`lib/db/{sqlite,postgres,mssql}.ts`) each run import-time side effects (dotenv, filesystem,
live connection objects) and have no mechanism today that would stop one of them from being
pulled into a client bundle. `lib/index.ts`'s barrel re-exports `auth.config.ts` alongside
genuinely client-safe modules, but nothing currently imports the barrel itself (see
proposal.md - Impact).

## Goals / Non-Goals

**Goals:**
- A leak of any of these four modules into a client bundle fails `pnpm build`, with an error
  that names the offending module, so the failure is loud and points at its cause.
- Every currently-working server-only call site keeps working, unchanged.

**Non-Goals:**
- Replicating the sibling kit's dynamic-`import()` rewrite. That worked around a Vite/
  TanStack Start-specific limitation (its import-protection plugin gates by file naming
  convention, and the leak had already reached the client bundle by the time SSR ran). Next's
  Server Component model already keeps this kit's actual call sites server-only by
  construction (see proposal.md - Impact); there is no equivalent runtime gap to route
  around here.
- Restructuring `lib/index.ts`'s barrel. It is unused for `auth.config.ts` today (confirmed
  by grep), so leaving it as-is ships no active bug. Removing the re-export would be a
  reasonable follow-up but is a separate, YAGNI-scoped decision from adding the missing
  guard; noted under Risks below instead of folded into this change.

## Decisions

**Use the `server-only` package, imported at the top of each guarded file.**

This is Next.js's own documented mechanism for exactly this problem: importing it makes the
module throw a build error if webpack/Turbopack ever includes it in a client bundle graph.
Alternatives considered:

- *Rely on convention/code review alone (status quo).* This is what the sibling kit had
  before its incident. Rejected — it is the exact gap this change exists to close.
- *A custom ESLint rule restricting imports of these files to server-only paths.* Would catch
  the mistake at lint time instead of build time, potentially earlier. Rejected for this
  change: it duplicates what `server-only` already does for free with zero new tooling, and
  a lint rule can be bypassed (`eslint-disable`) in a way a build failure cannot. Worth
  revisiting only if `server-only` proves to miss a real case in practice.
- *File-naming convention (`*.server.ts`), matching the sibling kit's own hardening.*
  TanStack Start recognizes that suffix natively; Next.js does not, so this would be a purely
  cosmetic rename with no build-time enforcement behind it here. Rejected as not actually
  solving the problem in this framework.

**Guard the three database adapters directly, not only `auth.config.ts`.**

`auth.config.ts` already composes all three, so guarding it alone would catch every path that
goes through it. Guarding the adapters too is defense in depth: if a future change imports
`lib/db/postgres.ts` directly for some unrelated reason (e.g., a health-check route), that
import gets the same protection without depending on it happening to go through
`auth.config.ts` first.

## Risks / Trade-offs

- [A future contributor sees `server-only`'s build error and "fixes" it by removing the
  import rather than fixing the actual client-side import that caused it] → The error message
  `server-only` produces names the file it was imported from, which is the guarded module,
  not the offending client component; mitigated by keeping this design doc and the code
  comment doing the guarding (see tasks.md) explicit about why the guard exists, so removing
  it is a visibly deliberate choice, not an easy misread.
- [The barrel's dead `export * from "./auth.config"` in `lib/index.ts` remains, still capable
  of pulling the graph in if someone imports the bare barrel in the future] → Not eliminated
  by this change; the `server-only` sentinel still catches it at build time even through the
  barrel, so the guard holds either way. Tightening the barrel itself is left as a
  possible follow-up, not required here.

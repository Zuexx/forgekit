// Fails the build if this module is ever pulled into a client bundle, rather than shipping
// broken client JS that only fails at runtime -- the sibling TanStack Start starter shipped
// exactly that failure mode once, from a module with this same import-time-side-effect shape.
// See openspec/changes/guard-auth-server-only-boundary/design.md for the full rationale.
//
// The real implementation lives in auth-instance.ts, not here -- the better-auth CLI (used by
// pnpm auth.generate/auth.migration and CI's schema step) loads its --config target directly,
// outside Next's webpack build, and cannot resolve this "server-only" import at all. Pointing
// the CLI at auth-instance.ts instead keeps this file's guard real for every app import while
// still letting the CLI load a working config.
import "server-only"

export * from "./auth-instance"
